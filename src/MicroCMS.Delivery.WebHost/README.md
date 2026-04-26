# MicroCMS — Delivery API

The **Delivery WebHost** is the public-facing, read-only content API of MicroCMS.  
It runs as a completely separate ASP.NET Core process from the Admin host, listening on its own port (default **5001**), and serves only **published** content to frontend applications.

---

## Table of Contents

1. [Architecture Overview](#1-architecture-overview)
2. [Project Structure](#2-project-structure)
3. [Authentication — API Keys](#3-authentication--api-keys)
4. [Rate Limiting](#4-rate-limiting)
5. [API Endpoints](#5-api-endpoints)
   - [Pages](#pages)
   - [Entries](#entries)
   - [Components](#components)
   - [Media](#media)
6. [Rendering Pipeline](#6-rendering-pipeline)
   - [Headless Mode (JSON)](#headless-mode-json)
   - [Server-Side Rendering (HTML)](#server-side-rendering-html)
   - [Full Page Render](#full-page-render)
   - [Component-Level Render](#component-level-render)
7. [Layout System](#7-layout-system)
8. [Multi-Tenancy & Site Isolation](#8-multi-tenancy--site-isolation)
9. [Media Streaming](#9-media-streaming)
10. [Error Handling](#10-error-handling)
11. [Configuration](#11-configuration)
12. [Running Locally](#12-running-locally)
13. [Dependency Map](#13-dependency-map)

---

## 1. Architecture Overview

```
┌──────────────────────────────────────────────────────────────────┐
│            MicroCMS.Delivery.WebHost         │
│     (port 5001)      │
│           │
│  ┌──────────────┐  ┌──────────────┐┌────────────────────────┐ │
│  │Pages       │  │  Entries     │  │  Components / Media    │ │
│  │  Controller  │  │  Controller  │  │  Controller            │ │
│  └──────┬───────┘  └──────┬───────┘  └───────────┬────────────┘ │
│       │      │           │    │
│         └─────────────────┴───────────────────────┘ │
│         │  MediatR   │
│     ▼     │
│        MicroCMS.Application (CQRS handlers)     │
│        │                 │
│   MicroCMS.Infrastructure (EF Core + Storage)          │
└──────────────────────────────────────────────────────────────────┘
         │
         ├── MicroCMS.Delivery.Core
     │       ├── Auth/   — X-Api-Key authentication handler
         │       └── Rendering/
      │               ├── ComponentRenderer   (Handlebars, stub for React/Web Component/Razor)
      │          ├── LayoutRenderer      (Handlebars, token-replace for HTML shells)
         │         └── ComponentRenderingService  (bridges Application ↔ renderers)
   │
         └── Shared Database (same DB as Admin host, filtered views via Tenant + Status)
```

### Design Principles

| Principle | How it is enforced |
|---|---|
| **Read-only** | No write endpoints exist; all Application queries are `IQuery<T>` — never wrapped by `UnitOfWorkBehavior` |
| **Published content only** | Every specification (`PublishedEntryBySlugSpec`, `AvailableMediaAssetsBySiteSpec`, etc.) filters by status at the database level |
| **Tenant isolation** | EF Core global query filters on every entity ensure row-level isolation per tenant |
| **Site isolation** | Every request requires `siteId`; the API key's embedded `site_id` claim is validated at auth time |
| **No admin dependency** | The Delivery WebHost references only `Application`, `Infrastructure`, `Domain`, `Shared`, and `Delivery.Core` — never `MicroCMS.Api` |

---

## 2. Project Structure

```
src/
├── MicroCMS.Delivery.WebHost/← Composition root (this project)
│   ├── Controllers/
│   │   ├── DeliveryControllerBase.cs← Base: route prefix, OkOrProblem helper
│   │   ├── PagesController.cs          ← Page tree + full-page render
│   │   ├── EntriesController.cs        ← Published entries (list + single)
│   │   ├── ComponentsController.cs     ← Component items (JSON + optional HTML)
│   │   └── MediaController.cs     ← Asset metadata + binary stream
│   └── Program.cs           ← DI wiring, middleware pipeline
│
├── MicroCMS.Delivery.Core/       ← Auth + rendering engines
│   ├── Auth/
│   │   ├── DeliveryApiKeyDefaults.cs
│   │   └── DeliveryApiKeyHandler.cs    ← X-Api-Key → ClaimsPrincipal
│   ├── Extensions/
│   │   └── DeliveryServiceCollectionExtensions.cs
│   └── Rendering/
│       ├── ComponentRenderer.cs        ← IComponentRenderer
│       ├── LayoutRenderer.cs← ILayoutRenderer
│    └── ComponentRenderingService.cs ← IComponentRenderingService (bridge)
│
└── MicroCMS.Application/
 └── Features/Delivery/
        ├── Dtos/       ← DeliveryEntryDto, DeliveryPageDto, RenderedPageDto …
        ├── Queries/    ← All IQuery<T> records
        └── Handlers/   ← Query handlers + RenderPageQueryHandler
```

---

## 3. Authentication — API Keys

Every endpoint is protected by the `DeliveryApiKey` authentication scheme.

### How it works

```
Request
  │
  ├─ Header:  X-Api-Key: <raw-key>
  │
  ▼
DeliveryApiKeyHandler.HandleAuthenticateAsync()
  │
  ├─ 1. Hash the raw key with ISecretHasher (PBKDF2 / HMAC)
  ├─ 2. Lookup ApiClient by hash  (ApiClientByHashSpec)
  ├─ 3. Check: IsActive == true AND (ExpiresAt == null OR ExpiresAt > UtcNow)
  ├─ 4. Build ClaimsPrincipal:
  │       tenant_id   = client.TenantId
  │       site_id     = client.SiteId
  │       auth_method = "api_key"
  │       client_id   = client.Id
  │       scope[]     = client.Scopes
  └─ 5. Return AuthenticationTicket
```

### Obtaining an API key

API keys are created in the **Admin** host:

```
Admin UI → Sites → {site} → API Clients → Create Client
```

The raw key is shown **once** on creation and never stored in plain text.  
MicroCMS stores only the **HMAC hash** — even a database breach cannot reveal keys.

### Claims available downstream

| Claim | Value |
|---|---|
| `tenant_id` | GUID of the owning tenant |
| `site_id` | GUID of the site this key is scoped to |
| `auth_method` | always `"api_key"` |
| `client_id` | GUID of the ApiClient record |
| `scope` | one claim per granted scope (e.g. `entries.read`, `media.read`) |

---

## 4. Rate Limiting

Token-bucket rate limiting is applied globally, keyed by **site_id** (from the API key claim) or remote IP as a fallback.

| Parameter | Default |
|---|---|
| Token limit | 500 requests |
| Replenishment | 500 tokens per minute |
| Queue limit | 20 requests |
| Over-limit response | `429 Too Many Requests` |

The key is extracted from the `site_id` claim so multiple API keys for the same site share one bucket, preventing circumvention via key rotation.

---

## 5. API Endpoints

Base route: `/delivery/v1/[controller]`  
All requests require `X-Api-Key` header.

### Pages

| Method | Route | Description |
|---|---|---|
| `GET` | `/delivery/v1/pages?siteId={id}` | Full page tree ordered by depth, then title |
| `GET` | `/delivery/v1/pages/{slug}?siteId={id}` | Single page node by slug |
| `GET` | `/delivery/v1/pages/{slug}/render?siteId={id}` | **Full server-side render** of a page |

The page node itself contains **no body content** — it is structural metadata only.

```json
// GET /delivery/v1/pages/blog
{
  "id": "3fa85f64...",
  "siteId": "...",
  "title": "Blog",
  "slug": "blog",
  "pageType": "Collection",
  "parentId": null,
  "linkedEntryId": null,
  "collectionContentTypeId": "abc-123",
  "routePattern": "/blog/{slug}",
  "depth": 0
}
```

**Page types:**

| `pageType` | Meaning | What to fetch next |
|---|---|---|
| `Static` | A single page backed by one entry | `GET /entries/{contentType}/{linkedEntryId}` |
| `Collection` | A listing of entries of a content type | `GET /entries/{contentTypeKey}` |

---

### Entries

| Method | Route | Description |
|---|---|---|
| `GET` | `/delivery/v1/entries/{contentTypeKey}?siteId={id}` | Paginated list of published entries |
| `GET` | `/delivery/v1/entries/{contentTypeKey}/{slug}?siteId={id}` | Single published entry by slug |

**Query parameters (list):**

| Parameter | Default | Description |
|---|---|---|
| `locale` | `null` (all locales) | BCP-47 locale filter, e.g. `en`, `fr-CA` |
| `page` | `1` | 1-based page number |
| `pageSize` | `50` | Max `200` |

**Response shape:**

```json
{
"id": "...",
  "slug": "my-post",
  "contentTypeKey": "blog_post",
  "locale": "en",
  "status": "Published",
  "fields": {
    "title": "Hello World",
    "body": "<p>...</p>",
    "coverImage": "https://..."
  },
  "seo": {
    "title": "Hello World | Blog",
    "description": "...",
    "ogImage": "https://...",
    "canonicalUrl": "https://..."
  },
  "publishedAt": "2025-01-15T10:00:00Z",
  "updatedAt": "2025-01-15T10:00:00Z"
}
```

`fields` is a **dynamic JSON object** — its shape is defined by the content type's field schema managed in the Admin.

---

### Components

| Method | Route | Description |
|---|---|---|
| `GET` | `/delivery/v1/components/{componentKey}?siteId={id}` | Paginated list of published component items |
| `GET` | `/delivery/v1/components/{componentKey}/{itemId}?siteId={id}` | Single published item — JSON or HTML |

The single-item endpoint supports **content negotiation**:

- `Accept: application/json` → returns `DeliveryComponentItemDto`
- `Accept: text/html` → renders the item via the component's template engine (currently **Handlebars** only; other types return a placeholder comment)

---

### Media

| Method | Route | Description |
|---|---|---|
| `GET` | `/delivery/v1/media?siteId={id}` | Paginated list of available assets with resolved URLs |
| `GET` | `/delivery/v1/media/{id}?siteId={id}` | Single asset metadata + URL |
| `GET` | `/delivery/v1/media/{id}/stream?siteId={id}` | Binary stream passthrough |

**URL resolution:**

| Asset visibility | URL type | Validity |
|---|---|---|
| `Public` | Direct CDN / storage URL | Permanent |
| `Private` | HMAC-signed URL | 1 hour |

**Binary stream (`/stream`):**  
Used when the storage backend has no public URL (e.g. local filesystem in dev).  
Private assets require `exp`, `tid`, and `sig` query parameters validated via HMAC-SHA256 before the stream opens.

---

## 6. Rendering Pipeline

The Delivery API supports two consumption modes, switchable per-request via the `Accept` header.

### Headless Mode (JSON)

The default. The API returns raw structured data; the frontend owns all HTML rendering.

```
Frontend (Next.js / Astro / Mobile)
│
    ├─ GET /delivery/v1/pages/{slug}         → page metadata
    ├─ GET /delivery/v1/entries/{type}/{slug}→ entry fields as JSON
    └─ GET /delivery/v1/components/{key}     → component items as JSON
```

**Best for:** JAMstack frontends, mobile apps, any framework that has its own templating.

---

### Server-Side Rendering (HTML)

The Delivery API can render HTML server-side when a component has a template configured.

#### Component-Level Render

```
GET /delivery/v1/components/hero-banner/{itemId}
Accept: text/html
```

Flow:

```
ComponentsController.Get()
  │
  ├─ Fetch ComponentItem via MediatR
  ├─ Detect Accept: text/html
  ├─ Load Component aggregate (schema + TemplateType + TemplateContent)
  └─ IComponentRenderer.RenderAsync(component, item)
        │
   ├─ Handlebars  → Handlebars.Net compiles + executes TemplateContent
        │ Fields JSON is flattened to {{heading}}, {{body}} etc.
        ├─ React       → <!-- component:key id:... type:React -->  (hydrate client-side)
        ├─ WebComponent→ <!-- component:key id:... type:WebComponent -->
        └─ RazorPartial→ <!-- component:key id:... type:RazorPartial -->
```

Only **Handlebars** templates are rendered server-side today. All other template types emit a placeholder comment that signals the frontend to hydrate the component client-side.

---

### Full Page Render

```
GET /delivery/v1/pages/{slug}/render?siteId={id}
Accept: text/html     → full HTML document
Accept: application/json   → RenderedPageDto with per-zone HTML fragments
```

This is the most complete rendering path. It walks the entire page composition graph:

```
RenderPageBySlugQuery
  │
  ├─ 1. PageBySlugSpec
  │       → Resolve Page by slug
  │
  ├─ 2. PageTemplateByPageSpec
  │       → Load PageTemplate  (zone map + ordered ComponentPlacements)
  │
  ├─ 3. For each ComponentPlacement (ordered by SortOrder within zone):
  │       ├─ Load Component  (schema + TemplateType + TemplateContent)
  │       ├─ Load all published ComponentItems for that component
  │       └─ IComponentRenderingService.RenderComponentAsync()
  │     └─ IComponentRenderer → HTML fragment
  │
  ├─ 4. Group fragments by zone name
  │       e.g.  { "hero-zone": "<div>...</div>", "content-zone": "<section>..." }
  │
  ├─ 5. ResolveLayout:
  │       page.LayoutId  → specific layout override for this page
  │  │ (none) → DefaultLayoutBySiteSpec → site-wide default layout
  │       └─ (none) → headless fallback (zones returned as JSON)
  │
  └─ 6. IComponentRenderingService.RenderLayoutAsync()
          └─ ILayoutRenderer → inject zones into Layout shell
                → full HTML document
```

**Response when a Layout is configured (`Accept: text/html`):**

```html
<!DOCTYPE html>
<html>
<head><title>My Page</title></head>
<body>
  <nav><!-- nav from layout shell --></nav>
  <main>
    <!-- {{zone:hero-zone}} replaced with rendered component HTML -->
    <div class="hero">...</div>
    <!-- {{zone:content-zone}} replaced -->
    <section>...</section>
  </main>
  <footer><!-- footer from layout shell --></footer>
</body>
</html>
```

**Response when no Layout is configured (`application/json` fallback):**

```json
{
  "pageId": "...",
  "slug": "home",
  "title": "Home",
  "html": null,
  "zones": {
    "hero-zone":    "<div class=\"hero\">...</div>",
    "content-zone": "<section>...</section>"
  },
  "seo": null
}
```

---

## 7. Layout System

A **Layout** is the master HTML shell that wraps a rendered page.

### Zone placeholders

Inside a Layout's `ShellTemplate`, zones are injected using token syntax:

```html
<!DOCTYPE html>
<html>
<head>
  <title>{{seo:title}}</title>
  <meta name="description" content="{{seo:description}}">
</head>
<body>
  <nav><!-- static nav here --></nav>

  <section class="hero">
    {{zone:hero-zone}}
  </section>

  <main>
    {{zone:content-zone}}
  </main>

  <aside>
    {{zone:sidebar}}
  </aside>

  <footer><!-- static footer here --></footer>
</body>
</html>
```

| Token | Replaced with |
|---|---|
| `{{zone:name}}` | Accumulated HTML of all component placements in that zone |
| `{{seo:title}}` | Page / entry SEO title |
| `{{seo:description}}` | Meta description |
| `{{seo:ogImage}}` | OpenGraph image URL |

### Layout template types

| Type | How rendered | Best for |
|---|---|---|
| `Handlebars` **(default)** | Compiled and executed via Handlebars.Net. Full logic support: `{{#if}}`, `{{#each}}`, partials. Zones injected as `{{{zone_hero_zone}}}` (triple-stash keeps HTML unescaped). | Any layout needing conditional zones, loops over nav items, or reusable partials |
| `Html` | Simple `string.Replace` on `{{zone:name}}` and `{{seo:*}}` tokens. No logic constructs. | Static layouts authored in a visual editor and pasted in; maximum simplicity |

> **Why not Razor?**  
> Razor templates stored in a database require runtime Roslyn compilation (heavy), a custom `IFileProvider` to feed DB strings into the view engine, and `IRazorViewEngine`/`ITempDataProvider` wiring tightly coupled to MVC internals. More critically, Razor allows arbitrary C# execution — which is a **remote code execution risk** when the template source is stored in a database editable by CMS users. Handlebars provides equivalent expressiveness (`{{#if}}`, `{{#each}}`, partials) in a sandboxed, logic-only template language with no code execution surface.

### Layout resolution order

1. `Page.LayoutId` — page-specific override
2. Site-default layout (`IsDefault = true`)
3. No layout → headless JSON response

### PageTemplate (zone map)

A `PageTemplate` maps `ComponentPlacement` records to a `Page`. Each placement specifies:

```
ComponentPlacement
  ├─ ComponentId  → which Component schema to use
  ├─ Zone      → which zone it renders into  (e.g. "hero-zone")
  └─ SortOrder    → order within the zone
```

Multiple components can be placed in the same zone; they are concatenated in `SortOrder` sequence.

---

## 8. Multi-Tenancy & Site Isolation

The Delivery API enforces **two levels of isolation on every request**:

### Tenant isolation (EF Core global query filters)

Every entity table has a `TenantId` column. A global query filter is registered per entity:

```csharp
modelBuilder.Entity<Entry>().HasQueryFilter(
    e => _tenantFilter == null || e.TenantId == _tenantFilter);
```

`_tenantFilter` is set from the `tenant_id` claim on the authenticated API key. A frontend application can never read another tenant's data — not even with a valid API key — because the filter is applied at the SQL level.

### Site isolation (query parameters + specifications)

Every endpoint requires an explicit `siteId` query parameter. All specifications (`PageBySlugSpec`, `PublishedEntriesByContentTypeSpec`, etc.) include `SiteId == siteId` in their `Criteria` expression.

The API key's `site_id` claim matches the requested `siteId`; the auth handler blocks mismatches at authentication time.

---

## 9. Media Streaming

The `/stream` endpoint proxies binary asset data from the underlying storage provider.

```
GET /delivery/v1/media/{id}/stream?siteId={id}
```

**Public assets** — validated via `X-Api-Key` only, streamed directly:

```
Client → Delivery API → IStorageProvider.DownloadAsync() → FileStreamResult
```

**Private assets** — additionally require a signed URL:

```
GET /delivery/v1/media/{id}/stream?siteId=...&exp=1700000000&tid=...&sig=abc123

IStorageSigningService.Validate(storageKey, exp, tid, sig)
  └─ HMAC-SHA256(storageKey + exp + tid, signingKey) == sig
  └─ exp > UtcNow
  └─ tid matches authenticated tenant
```

Signed URLs are generated by the Asset metadata endpoint (`GET /media/{id}`) and are valid for **1 hour**. The stream endpoint supports HTTP range requests (`Range` header) for video/audio seeking.

---

## 10. Error Handling

All errors are returned as **RFC 7807 Problem Details** (`application/problem+json`).

| Exception type | HTTP status |
|---|---|
| `NotFoundException` | `404 Not Found` |
| `ConflictException` | `409 Conflict` |
| `ForbiddenException` | `403 Forbidden` |
| `UnauthorizedException` | `401 Unauthorized` |
| `ValidationException` | `422 Unprocessable Entity` |
| `DomainException` | `400 Bad Request` |
| Unhandled `Exception` | `500 Internal Server Error` |

```json
// Example 404
{
  "type": "https://tools.ietf.org/html/rfc7807",
  "title": "Entry.NotFound",
  "status": 404,
  "detail": "Entry 'my-missing-slug' was not found."
}
```

---

## 11. Configuration

### `appsettings.json` keys

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "..."
  },
  "Storage": {
    "Provider": "AzureBlob | S3 | Filesystem",
    "ConnectionString": "...",
  "Container": "microcms-media"
  },
  "Signing": {
    "SecretKey": "<32-byte base64 key for HMAC signed URLs>"
  },
  "Serilog": {
    "MinimumLevel": { "Default": "Information" }
  }
}
```

### Environment-specific overrides

`appsettings.{Environment}.json` is loaded automatically by ASP.NET Core. Secrets should be injected via environment variables in production (the config pipeline calls `AddEnvironmentVariables()` last, so env vars always win).

---

## 12. Running Locally

### Prerequisites

- .NET 8 SDK
- A running instance of the **Admin WebHost** (or a seeded database) to have content to deliver
- At least one **API Client** created in the Admin for the target site

### Start the Delivery host

```bash
# From solution root
dotnet run --project src/MicroCMS.Delivery.WebHost

# With explicit environment
ASPNETCORE_ENVIRONMENT=Development dotnet run --project src/MicroCMS.Delivery.WebHost
```

Default URL: `https://localhost:5001`

### Swagger UI (Development only)

```
https://localhost:5001/swagger
```

Click **Authorize** and enter your `X-Api-Key` value to test endpoints interactively.

### Health check

```bash
curl https://localhost:5001/health
# → Healthy
```

### Example requests

```bash
# List page tree
curl -H "X-Api-Key: your-key" \
  "https://localhost:5001/delivery/v1/pages?siteId=00000000-0000-0000-0000-000000000001"

# Get a published blog post
curl -H "X-Api-Key: your-key" \
  "https://localhost:5001/delivery/v1/entries/blog_post/my-first-post?siteId=..."

# Render a full page as HTML
curl -H "X-Api-Key: your-key" \
     -H "Accept: text/html" \
  "https://localhost:5001/delivery/v1/pages/home/render?siteId=..."

# Get a component item rendered as HTML (Handlebars template)
curl -H "X-Api-Key: your-key" \
   -H "Accept: text/html" \
  "https://localhost:5001/delivery/v1/components/hero-banner/3fa85f64-...?siteId=..."
```

---

## 13. Dependency Map

```
MicroCMS.Delivery.WebHost
  ├── MicroCMS.Delivery.Core   ← Auth + rendering engines
  │     ├── MicroCMS.Application      ← CQRS queries + IComponentRenderingService
  │     ├── MicroCMS.Domain       ← Aggregates + Specifications
  │     └── MicroCMS.Shared           ← IDs, Result<T>, PagedList<T>
  │
  ├── MicroCMS.Application
  │     ├── MicroCMS.Domain
  │     └── MicroCMS.Shared
  │
  └── MicroCMS.Infrastructure   ← EF Core + Storage + repository implementations
     ├── MicroCMS.Application
        ├── MicroCMS.Domain
        └── MicroCMS.Shared
```

**What the Delivery WebHost does NOT reference:**

- `MicroCMS.Api` — admin REST controllers
- `MicroCMS.GraphQL` — admin GraphQL layer
- `MicroCMS.Admin.WebHost` — admin composition root
- Any write-side application commands

This enforces a hard architectural boundary: the delivery surface can never accidentally expose a write endpoint.
