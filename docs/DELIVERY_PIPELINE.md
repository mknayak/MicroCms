# MicroCMS Delivery Pipeline

This document describes the end-to-end journey from an HTTP page request to a fully rendered HTML response. It covers page assembly, component rendering, layout rendering, and token resolution at both the component and layout scopes.

---

## Table of Contents

1. [Overview](#1-overview)
2. [Pipeline Stages at a Glance](#2-pipeline-stages-at-a-glance)
3. [Stage 1 — Resolve the Page](#3-stage-1--resolve-the-page)
4. [Stage 2 — Load the PageTemplate](#4-stage-2--load-the-pagetemplate)
5. [Stage 3 — Render Component Zones](#5-stage-3--render-component-zones)
   - [5.1 Component Rendering Internals](#51-component-rendering-internals)
   - [5.2 Tokens Inside Components](#52-tokens-inside-components)
6. [Stage 4 — Resolve SEO](#6-stage-4--resolve-seo)
7. [Stage 5 — Resolve the Layout](#7-stage-5--resolve-the-layout)
8. [Stage 6 — Build the RenderContext](#8-stage-6--build-the-rendercontext)
9. [Stage 7 — Render the Layout Shell](#9-stage-7--render-the-layout-shell)
   - [9.1 Zone Injection](#91-zone-injection)
   - [9.2 Layout Rendering Strategies](#92-layout-rendering-strategies)
10. [Stage 8 — Token Resolution Pipeline](#10-stage-8--token-resolution-pipeline)
    - [10.1 Token Syntax](#101-token-syntax)
    - [10.2 Pipeline Algorithm](#102-pipeline-algorithm)
    - [10.3 Namespace Resolvers](#103-namespace-resolvers)
    - [10.4 Caching and the user:\* Restriction](#104-caching-and-the-user-restriction)
    - [10.5 Unresolved Token Fallback](#105-unresolved-token-fallback)
11. [Stage 9 — Compose the Response](#11-stage-9--compose-the-response)
12. [Component Tokens vs. Layout Tokens — Summary](#12-component-tokens-vs-layout-tokens--summary)
13. [Data-Flow Diagram](#13-data-flow-diagram)
14. [Key Classes Reference](#14-key-classes-reference)

---

## 1. Overview

The delivery pipeline is responsible for turning a URL slug into a renderable HTML page. At a high level it:

- Looks up domain objects (Page, PageTemplate, Components, Entries, Layout) from the repository layer.
- Renders each **component** into an HTML fragment using that component's own template and field data.
- Assembles those fragments into named **zones**.
- Injects the zones into the **layout shell** template.
- Runs the **token resolution pipeline** over the resulting shell to substitute dynamic `{{namespace:key}}` placeholders.
- Returns a `RenderedPageDto` that wraps the final HTML (or zone-only output when no layout is present).

The entry point is **`RenderPageBySlugQueryHandler`** in the Application layer, and all actual rendering work is delegated to services in **`MicroCMS.Delivery.Core`**.

---

## 2. Pipeline Stages at a Glance

```
HTTP Request (slug)
        │
        ▼
[1]  Resolve Page          – look up Page by site + slug
        │
        ▼
[2]  Load PageTemplate     – find PageTemplate for that Page
        │
        ▼
[3]  Render Component Zones
       ├─ foreach Placement (ordered by SortOrder)
       │     ├─ Load Component + its published Entries
       │     └─ ComponentRenderer.RenderAsync(component, item)
       │           ├─ Handlebars:  escape {{ns:key}} → render fields → HTML fragment
       │           │               (namespace tokens survive into zone HTML)
       │           └─ Other types: emit hydration hint comment
       │
        ▼
[4]  Resolve SEO           – Page.Seo overrides, Page.Title as fallback
        │
        ▼
[5]  Resolve Layout        – Page.LayoutId → site default Layout
        │
        ▼
[6]  Build RenderContext   – populate slug, title, template key, tenant/site IDs
        │
        ▼
[7]  Render Layout Shell
       ├─ Handlebars: compile(ShellTemplate) with {zones, seo} data
       └─ Html:       string.Replace({{zone:*}}, {{seo:*}}) literal substitutions
        │
        ▼
[8]  Token Resolution Pipeline
       ├─ Extract  {{namespace:key}} tokens
       ├─ Warn     user:* tokens (not resolved, emits HTML comment)
       ├─ Execute  resolvers sequentially (page, template, seo, site, user)
       ├─ Cache    cacheable namespaces within the request
       └─ Substitute: resolved → value, unresolved → <!-- token not found: … -->
        │
        ▼
[9]  Return RenderedPageDto  (html, seo, slug, title)
```

---

## 3. Stage 1 — Resolve the Page

```
pageRepo.ListAsync(new PageBySlugSpec(siteId, slug))
```

- A `PageBySlugSpec` specification is used to query a page that belongs to the requested `SiteId` and matches the URL slug exactly.
- Throws `NotFoundException` if no page is found so the delivery host can return a 404.

---

## 4. Stage 2 — Load the PageTemplate

```
templateRepo.ListAsync(new PageTemplateByPageSpec(page.Id))
```

- A `PageTemplate` holds an ordered list of **`ComponentPlacements`** — each placement references a `Component` and assigns it to a named **zone**.
- A page without a template produces no zones; zone rendering is skipped.

---

## 5. Stage 3 — Render Component Zones

Implemented in `RenderPageBySlugQueryHandler.RenderZonesAsync`.

### Processing Order

1. Placements are iterated in ascending `SortOrder`.
2. For each placement:
   - The `Component` aggregate is loaded from `compRepo`.
   - Published `Entry` items belonging to the component's backing content type are fetched from `entryRepo`.
   - Each `(Component, Entry)` pair is passed to `IComponentRenderingService.RenderComponentAsync`.
3. HTML fragments from the same zone name are **concatenated** using a `StringBuilder`.
4. The result is a `Dictionary<string, string>` (zone name → accumulated HTML).

### 5.1 Component Rendering Internals

`ComponentRenderer` (in `MicroCMS.Delivery.Core`) handles the actual rendering:

| `RenderingTemplateType` | Behaviour |
|---|---|
| `Handlebars` | Compiles `Component.TemplateContent` with HandlebarsDotNet; data model is the entry's field JSON flattened into a `Dictionary<string, object?>`. |
| `React` | Emits `<!-- component:key id:... type:React -->` — client-side hydration comment. |
| `WebComponent` | Emits a hydration hint comment. |
| `RazorPartial` | Emits a hydration hint comment; actual rendering is delegated to an MVC host via `Html.PartialAsync(component.Key)`. |

**Field flattening (`BuildDataDictionary`):**

The entry's `Fields` property is a `JsonElement` object. Each JSON property is mapped to its native .NET type:

```
string  → string
number  → long (or double if fractional)
true    → bool true
false   → bool false
null    → null
other   → .ToString()
```

This allows Handlebars templates to bind directly to field names, e.g. `{{heading}}`, `{{imageUrl}}`.

### 5.2 Tokens Inside Components

Component templates use **Handlebars-native binding** (`{{fieldName}}`). The data available at render time is limited to the entry's own fields.

#### `{{namespace:key}}` pass-through

`ComponentRenderer` automatically escapes any `{{namespace:key}}` token it finds in the template source before Handlebars compiles it. Handlebars interprets the escape prefix `\{{` as a literal and emits the token text unchanged into the output HTML.

```
Component template:  <h1>{{heading}}</h1><span>{{page:title}}</span>
Field dictionary:    { "heading": "Hello" }
After escape:        <h1>{{heading}}</h1><span>\{{page:title}}</span>
After Handlebars:    <h1>Hello</h1><span>{{page:title}}</span>  ← preserved
Zone HTML → Layout:  <h1>Hello</h1><span>{{page:title}}</span>
Token pipeline:      <h1>Hello</h1><span>My Page</span>         ← resolved
```

The rule of thumb is simple: **field bindings are resolved in the component, namespace tokens are resolved in the layout shell**. Any token that is not resolved by any registered resolver (including unrecognised namespaces) is replaced by an HTML comment by the layout token pipeline — it is never silently erased.

---

## 6. Stage 4 — Resolve SEO

```csharp
ResolveSeoAsync(page)
```

SEO values are resolved from the `Page` aggregate using a priority chain:

| Priority | Source | Field |
|---|---|---|
| 1 (highest) | `Page.Seo.MetaTitle` | `seo:title` |
| 2 (fallback) | `Page.Title` | `seo:title` (only when MetaTitle is null) |
| 1 | `Page.Seo.MetaDescription` | `seo:description` |
| 1 | `Page.Seo.OgImage` | `seo:ogImage` |
| 1 | `Page.Seo.CanonicalUrl` | canonical URL in response DTO |

The result is a `SeoDto` that is later both injected into the layout data model and exposed through `SeoTokenResolver`.

---

## 7. Stage 5 — Resolve the Layout

```csharp
ResolveLayoutAsync(page, siteId)
```

Resolution follows a two-level fallback:

1. **Page-level override** — if `Page.LayoutId` is set, that layout is loaded first.
2. **Site default** — if no page override exists (or the layout was not found), the site's default layout (`DefaultLayoutBySiteSpec`) is used.
3. **No layout** — if neither exists, `html` is `null` and the `RenderedPageDto` contains raw zone HTML instead.

---

## 8. Stage 6 — Build the RenderContext

```csharp
var renderContext = new RenderContext
{
    SiteId       = siteId,
    TenantId     = page.TenantId,
    PageSlug     = page.Slug.Value,
    PageTitle    = page.Title,
    TemplateKey  = layout?.Key,
    TemplateName = layout?.Name,
};
```

`RenderContext` is the shared read-only bag that every token resolver reads from. It carries:

| Property | Exposed as token |
|---|---|
| `PageSlug` | `page:slug` |
| `PageTitle` | `page:title` |
| `PagePublishedAt` | `page:publishedAt` |
| `TemplateKey` | `template:key` |
| `TemplateName` | `template:name` |
| `SiteId` | `site:id` |
| `TenantId` | `site:tenantId` |
| `UserId` | `user:id` _(not resolved — see §10.4)_ |
| `UserName` | `user:name` _(not resolved — see §10.4)_ |
| `UserRole` | `user:role` _(not resolved — see §10.4)_ |

---

## 9. Stage 7 — Render the Layout Shell

`LayoutRenderer.RenderAsync` (in `MicroCMS.Delivery.Core`) is called by `ComponentRenderingService.RenderLayoutAsync`, which is the bridge between the Application and Delivery.Core layers.

### 9.1 Zone Injection

Zone HTML is injected into the shell **before** token resolution runs. This ensures that any tokens emitted by component HTML fragments are also resolved in the final pass.

### 9.2 Layout Rendering Strategies

| `LayoutTemplateType` | Zone injection | SEO injection |
|---|---|---|
| **Handlebars** | Zones exposed as `{{zone_hero_zone}}` (hyphens → underscores) and `{{zones.hero-zone}}`; SEO as `{{seo.title}}`, `{{seo.description}}`, `{{seo.ogImage}}` |
| **Html** | Literal `{{zone:hero-zone}}` → `string.Replace`; `{{seo:title}}` → `string.Replace` |

After zone and SEO injection, the resulting string is passed to the **Token Resolution Pipeline** (Stage 8) for full `{{namespace:key}}` substitution.

> **Note:** For `Html` layout type, the SEO tokens are already substituted during zone injection via `string.Replace`. They will appear pre-resolved before the token pipeline runs, so `SeoTokenResolver` will find no remaining `seo:*` tokens to replace. For `Handlebars` layout type, SEO is resolved natively via the Handlebars data model, not via the token pipeline.

---

## 10. Stage 8 — Token Resolution Pipeline

`TokenResolutionPipeline.ResolveAsync` runs over the zone-injected shell string.

### 10.1 Token Syntax

Tokens follow the pattern `{{namespace:key}}`:

```
{{page:title}}           Current page title
{{page:slug}}            URL slug
{{page:publishedAt}}     ISO-8601 publish date (or empty)
{{template:key}}         Layout machine key
{{template:name}}        Layout display name
{{seo:title}}            Resolved SEO title
{{seo:description}}      Meta description
{{seo:ogImage}}          OpenGraph image URL
{{site:id}}              Site identifier
{{site:tenantId}}        Tenant identifier
{{user:id}}              ⚠ NOT resolved in layout scope (see §10.4)
{{user:name}}            ⚠ NOT resolved in layout scope (see §10.4)
{{user:role}}            ⚠ NOT resolved in layout scope (see §10.4)
```

### 10.2 Pipeline Algorithm

```
1. ExtractTokens(template)
      └─ Regex: \{\{([a-zA-Z][...]:...)\}\}
      └─ Deduplicated, case-insensitive

2. WarnUserNamespaceTokens(tokens)
      └─ LogWarning for every user:* token found

3. RunResolversAsync(tokens, context)
      foreach resolver in registered order:
        a. Filter tokens where token starts with resolver.Namespace + ":"
        b. If resolver.Cacheable AND all namespace tokens already in cache → skip
        c. Call resolver.ResolveAsync(namespaceTokens, context)
        d. Merge results into the shared resolved dictionary
        e. Catch and log any resolver exception (other resolvers still run)

4. Substitute(template, resolved)
      └─ user:*        → <!-- token not resolved: {token} (user:* not available in shell context) -->
      └─ resolved key  → resolved value
      └─ not found     → <!-- token not found: {token} -->
```

### 10.3 Namespace Resolvers

Resolvers are registered in DI as `ITokenResolver` and executed in registration order:

| Resolver | Namespace | Cacheable | Data Source |
|---|---|---|---|
| `PageTokenResolver` | `page` | ✅ | `RenderContext.PageSlug/Title/PublishedAt` |
| `TemplateTokenResolver` | `template` | ✅ | `RenderContext.TemplateKey/TemplateName` |
| `SeoTokenResolver` | `seo` | ✅ | Properties set by the query handler before the pipeline runs |
| `SiteTokenResolver` | `site` | ✅ | `RenderContext.SiteId/TenantId` |
| `UserTokenResolver` | `user` | ❌ | _Never invoked; user:* tokens are only warned_ |

`SeoTokenResolver` is special — its `SeoTitle`, `SeoDescription`, and `SeoOgImage` properties are injected by the query handler (from the resolved `SeoDto`) before the pipeline runs. This bridges the SEO resolution (Stage 4) with the token pipeline (Stage 8).

### 10.4 Caching and the `user:*` Restriction

**Caching** operates at the per-request level (not cross-request). For resolvers with `Cacheable = true`, if all tokens for a namespace are already present in the resolved dictionary, the resolver is skipped entirely. This avoids redundant resolver calls when the same token appears multiple times in the template.

**`user:*` tokens are intentionally never resolved in layout/shell templates.** The layout shell is a shared, potentially cacheable structure. Injecting user-specific values (session ID, name, role) into it would:
- Break any output caching applied to the layout.
- Risk leaking user context between requests in edge-cached environments.

If user-contextual rendering is required, it must be handled in a **component** template using client-side hydration (React/WebComponent types) or a Razor Partial where the host MVC framework controls per-request rendering.

### 10.5 Unresolved Token Fallback

Any token that no resolver returns a value for is replaced with an HTML comment:

```html
<!-- token not found: site:unknownField -->
```

`user:*` tokens get a more descriptive comment:

```html
<!-- token not resolved: user:name (user:* not available in shell context) -->
```

These comments are safe to ship to the browser (they are invisible to the end user) and easy to diagnose in View Source.

---

## 11. Stage 9 — Compose the Response

```csharp
return Result.Success(new RenderedPageDto(
    page.Id.Value,
    page.Slug.Value,
    page.Title,
    html,                              // null if no layout
    html is null ? zones : null,       // raw zones if no layout
    seo));
```

| Field | Content |
|---|---|
| `Html` | Fully rendered HTML page (layout + zones + resolved tokens). `null` if no layout was found. |
| `Zones` | Raw zone-name → HTML dictionary. Only populated when `Html` is null. Useful for headless consumers that apply their own shell. |
| `Seo` | The resolved `SeoDto` (title, description, ogImage, canonical URL). |
| `Slug`, `Title` | Metadata from the `Page` aggregate. |

---

## 12. Component Tokens vs. Layout Tokens — Summary

| Concern | Component scope | Layout shell scope |
|---|---|---|
| Template engine | Handlebars (field data) | Handlebars or HTML literal |
| Field binding syntax | `{{fieldName}}` (Handlebars native) | N/A |
| Namespace token syntax | `{{namespace:key}}` — escaped before Handlebars, **passed through** to layout | `{{namespace:key}}` — resolved by token pipeline |
| Data available at render time | Entry field JSON only | `RenderContext` fields, SEO, site, page, template |
| `{{namespace:key}}` resolution | Deferred — resolved after zone injection by the layout token pipeline | Resolved directly by the token pipeline |
| User-specific data | Must be client-side hydrated | ❌ Blocked — `user:*` never resolved |
| Fallback for missing values | Passed through to layout; falls back to `<!-- token not found: … -->` there | HTML comment `<!-- token not found: … -->` |
| Per-request caching | N/A | ✅ Cacheable resolvers skipped if all tokens already resolved |

---

## 13. Data-Flow Diagram

```
┌────────────────────────────────────────────────────────────────────────┐
│                    RenderPageBySlugQueryHandler                        │
│                                                                        │
│  Page ──────┐                                                          │
│  PageTemplate ─► Placements ─► foreach placement                      │
│                                   │                                   │
│                         ┌─────────▼──────────┐                        │
│                         │  ComponentRenderer  │                        │
│                         │  ─────────────────  │                        │
│                         │  Fields JSON        │                        │
│                         │  + Handlebars tmpl  │                        │
│                         │  ──────────────────►│ HTML fragment          │
│                         └─────────────────────┘                        │
│                                   │                                   │
│                         Accumulated into zones{}                       │
│                                   │                                   │
│  Page.Seo ──► ResolveSeoAsync ────┤                                   │
│                                   │                                   │
│  Layout ──────────────────────────┤                                   │
│  RenderContext ────────────────── ▼                                   │
│                         ┌──────────────────────┐                      │
│                         │    LayoutRenderer     │                      │
│                         │    ──────────────     │                      │
│                         │  1. Inject zones      │                      │
│                         │  2. Inject seo        │                      │
│                         │  3. ──────────────►   │ zone-resolved shell  │
│                         └──────────────────────┘                      │
│                                   │                                   │
│                         ┌─────────▼─────────────┐                     │
│                         │ TokenResolutionPipeline│                     │
│                         │ ─────────────────────  │                     │
│                         │  Extract tokens        │                     │
│                         │  Warn user:*           │                     │
│                         │  Run resolvers:        │                     │
│                         │    page → template     │                     │
│                         │    seo  → site         │                     │
│                         │    (user: skipped)     │                     │
│                         │  Substitute / fallback │                     │
│                         └───────────────────────┘                     │
│                                   │                                   │
│                         RenderedPageDto (html, seo, zones)             │
└────────────────────────────────────────────────────────────────────────┘
```

---

## 14. Key Classes Reference

| Class / Interface | Project | Responsibility |
|---|---|---|
| `RenderPageBySlugQueryHandler` | `MicroCMS.Application` | Orchestrates all 9 pipeline stages |
| `IComponentRenderingService` | `MicroCMS.Application` | Application-layer abstraction for component + layout rendering |
| `ComponentRenderer` | `MicroCMS.Delivery.Core` | Renders a single `(Component, Entry)` pair to HTML |
| `LayoutRenderer` | `MicroCMS.Delivery.Core` | Injects zones into a layout shell, then triggers token resolution |
| `TokenResolutionPipeline` | `MicroCMS.Application` | Extracts, resolves, caches, and substitutes `{{ns:key}}` tokens |
| `RenderContext` | `MicroCMS.Application` | Per-request data bag shared across all token resolvers |
| `ITokenResolver` | `MicroCMS.Application` | Contract for namespace-scoped token resolvers |
| `PageTokenResolver` | `MicroCMS.Application` | Resolves `page:*` tokens from `RenderContext` |
| `TemplateTokenResolver` | `MicroCMS.Application` | Resolves `template:*` tokens from `RenderContext` |
| `SeoTokenResolver` | `MicroCMS.Application` | Resolves `seo:*` tokens from injected SEO values |
| `SiteTokenResolver` | `MicroCMS.Application` | Resolves `site:*` tokens from `RenderContext` |
| `UserTokenResolver` | `MicroCMS.Application` | Declared but never invoked; `user:*` emits HTML comment warnings |
