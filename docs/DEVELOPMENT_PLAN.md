# MicroCMS — Development Plan

**Version:** 1.6
**Last Updated:** 2026-04-23
**Sprint Cadence:** 2 weeks
**Target GA:** Sprint 16 (~8.5 months from kickoff)
**Current Status:** Sprint 9 in progress — Search and Cache infrastructure scaffolded

---

## Guiding Principles

Every sprint produces working, tested, deployable software. No sprint ends with unbuildable code.
Architecture tests (`MicroCMS.Architecture.Tests`) run on every PR and block merge on failure.
Coverage gate: ≥ 80% on Domain and Application layers from Sprint 2 onward.
Security review is embedded in every sprint — not deferred to a "security sprint".
Cyclomatic complexity ≤ 10 / cognitive complexity ≤ 15 enforced via `.editorconfig` + SonarAnalyzer.

---

## Phase 0 — Foundation (Sprints 0–1)

### Sprint 0 — Project Scaffolding ✅ DONE
- Solution file, all `src/` and `tests/` projects created.
- Project references wired per §7.1 dependency rules.
- NuGet packages pinned per tech-stack decisions.
- `Directory.Build.props`, `.editorconfig`, `.gitignore` in place.
- Shared primitives: `Result<T>`, `Error`, `PagedList<T>`, `Guard`, strongly-typed IDs.
- Domain base classes: `AggregateRoot`, `Entity<TId>`, `ValueObject`, `IDomainEvent`.
- Application interfaces: `IUnitOfWork`, `ICurrentUser`, `ICacheService`, `IEventBus`, `IDateTimeProvider`.
- MediatR pipeline behaviors: `LoggingBehavior`, `ValidationBehavior`.
- AI abstractions: all four provider interfaces + DTOs.
- Plugin contract: `IPlugin`, `PluginManifest`.
- Architecture test stubs with NetArchTest.
- `appsettings.json` / `appsettings.Development.json`.

### Sprint 1 — Core Domain Model ✅ DONE
**Goal:** Full domain model for Content, Tenant, Identity, and Taxonomy aggregates — zero infrastructure dependencies.

Deliverables:
- `Tenant` aggregate root + value objects (`TenantSlug`, `TenantSettings`, `CustomDomain`). ✅
- `Site` entity within Tenant aggregate. ✅
- `ContentType` aggregate (field schema definitions, field type enum). ✅
- `Entry` aggregate (title, slug, status, locale, version chain, scheduled dates, author). ✅
- `EntryVersion` entity with diff support. ✅
- `MediaAsset` aggregate (file metadata, folder path, tags, EXIF). ✅
- `Taxonomy` aggregate (Category, Tag). ✅
- `User` / `Role` aggregates scoped to tenant. ✅
- Domain events for each significant state change (e.g. `EntryPublishedEvent`, `TenantCreatedEvent`). ✅
- Domain services: `SlugGenerator`, `LocaleFallbackChain`. ✅
- Specification classes for common queries. ✅
- Unit tests covering all aggregate invariants (≥ 80% coverage). ✅

Security items:
- All string value objects reject null/empty and enforce max-length invariants. ✅
- Slug sanitization: whitelist of allowed characters only. ✅

---

## Phase 1 — Infrastructure & REST API (Sprints 2–4)

### Sprint 2 — Persistence and EF Core ✅ DONE
**Goal:** Working database layer with SQLite for development and PostgreSQL for CI.

Deliverables:
- `ApplicationDbContext` with multi-tenant query filter (instance-field closure pattern — re-evaluated per query). ✅
- EF Core entity type configurations for all 9 aggregates (owned entities, value converters, indexes). ✅
- `EfRepository<T>` implementing `IRepository<TEntity, TId>` backed by EF Core. ✅
- `UnitOfWork` wrapping `SaveChangesAsync`; domain events written to outbox atomically. ✅
- `DomainEventsToOutboxInterceptor` — `SaveChangesInterceptor` serialising domain events to `OutboxMessage` rows. ✅
- `OutboxMessage` entity (type, content, tenantId, retry tracking). ✅
- `SpecificationEvaluator` translating `ISpecification<T>` to EF Core LINQ. ✅
- SQLite and PostgreSQL initial migrations. ✅
- `ApplicationDbContextFactory` (design-time factory). ✅
- `HttpContextCurrentUser`, `SystemDateTimeProvider`. ✅
- `DependencyInjection.AddInfrastructure()`. ✅
- Infrastructure integration tests (Testcontainers.PostgreSql). ✅

Security items:
- Parameterised queries only. ✅
- Tenant filter via `HasQueryFilter`; cross-tenant isolation verified by integration tests. ✅

### Sprint 3 — Application Layer: Content CQRS ✅ DONE
**Goal:** All content CRUD operations implemented end-to-end through Application layer.

Deliverables:
- `ICommand<T>` / `ICommand` / `IQuery<T>` marker interfaces. ✅
- Entry commands + queries + validators + handlers. ✅
- MediatR pipeline: `LoggingBehavior → AuthorizationBehavior → ValidationBehavior → UnitOfWorkBehavior → Handler`. ✅
- `HasPolicyAttribute`, `ContentPolicies`, `Roles`, `RolePermissions`. ✅
- `AuthorizationBehavior` — fail-secure (missing policy throws at runtime). ✅
- `Application/DependencyInjection.AddApplication()`. ✅
- 57 application unit tests. ✅

Security items:
- Every command/query requires `[HasPolicy]`; absence is a runtime error. ✅
- `UnauthorizedException` (401) vs `ForbiddenException` (403) cleanly separated. ✅

### Sprint 4 — REST API + Swagger ✅ DONE
**Goal:** Full HTTP API surface for content, tenants, and media, ready for consumer integration.

Deliverables:
- `ApiControllerBase` with `OkOrProblem` / `CreatedOrProblem` / `NoContentOrProblem` helpers. ✅
- `TenantsController`, `ContentTypesController`, `EntriesController`, `MediaController`, `TaxonomyController`. ✅
- Application feature handlers for all domains (Tenants, ContentTypes, Media, Taxonomy). ✅
- Domain specifications: `ContentTypesBySiteSpec`, `MediaAssetsBySiteSpec`, `TaxonomyBySiteSpec`, `AllTenantsSpec`. ✅
- `ConflictException` → 409; `QuotaExceededException` → 429 in Problem Details. ✅
- API versioning (`/api/v1/`), Swagger/OpenAPI 3.0 with JWT Bearer security definition. ✅
- Problem Details (RFC 7807) middleware. ✅
- Rate limiting: token-bucket per tenant-ID / IP. ✅
- JWT bearer authentication + CORS. ✅
- Health check endpoints (`/health/live`, `/health/ready`). ✅
- WebHost wiring fully implemented. ✅
- 11 contract tests passing. ✅

Security items:
- All endpoints annotated with `[Authorize]`. ✅
- HSTS, HTTPS redirect, correlation ID, security response headers. ✅

---

## Phase 2 — Multi-Tenancy & Admin UI (Sprints 5–6)

### Sprint 5 — Multi-Tenancy Hardening ✅ DONE
**Goal:** Tenant isolation tested under adversarial conditions; tenant onboarding flow complete.

Deliverables:
- `SubdomainTenantResolver` — subdomain → TenantId with LRU cache (5-min TTL, 1 024-entry cap). ✅
- `TenantResolutionMiddleware` — resolves tenant early in pipeline; exempts `/health`, `/swagger`, `/metrics`. ✅
- `TenantOnboardingService` — atomically provisions Tenant (Provisioning→Active) + default site + admin User. ✅
- `OnboardTenantCommand` + handler. ✅
- `QuotaService` — enforces `MaxUsers`, `MaxSites`, `MaxContentTypes` (zero = unlimited). ✅
- `QuotaExceededException` → HTTP 429. ✅
- `TenantAdminController` (`/api/v1/admin/tenants`) — full CRUD + onboard + site management. ✅
- `UsersController` (`/api/v1/users`) — invite, assign/revoke roles, deactivate. ✅
- User management commands/queries + `UserSpecs`. ✅
- 3 adversarial integration tests (cross-tenant isolation, onboarding, quota). ✅
- Contract tests extended to 11 (401 on all protected routes, security headers, correlation ID, spoof guard). ✅

Security items:
- `X-Tenant-Slug` header only honoured for `SystemAdmin` JWT role. ✅
- `TenantResolutionMiddleware` registered before authentication. ✅

Implementation notes:
- Schema-per-tenant `IMigrationRunner` deferred to Sprint 7 (requires identity layer). ✅
- Outbox `TenantId` validation on dispatch deferred to Sprint 12 (Webhooks). ✅

### Sprint 6 — Admin UI (React)
**Goal:** Fully functional browser-based admin portal backed by the REST API; served from `MicroCMS.Admin.WebHost`.

Tech stack: **React 18 + Vite + TypeScript + Tailwind CSS + TanStack Query + React Hook Form + Zod**.

Deliverables:
- Vite + React + TypeScript scaffold inside `src/MicroCMS.Admin.WebHost/ClientApp/`.
- ASP.NET Core SPA proxy in `MicroCMS.Admin.WebHost` serving the Vite dev server in Development and the built `dist/` in Production.
- Authentication: JWT login form → stores access token in `sessionStorage`; auto-attaches `Authorization: Bearer` header via Axios interceptor.
- **Dashboard** — tenant summary cards (entry count, media usage, user count).
- **Content Types** — list, create, add/remove fields (drag-to-reorder), publish, archive.
- **Entries** — paginated list with status filter; rich text editor (TipTap) for body fields; publish / unpublish / schedule workflow buttons; version history drawer.
- **Media Library** — grid/list toggle, drag-and-drop upload (calls `POST /api/v1/media`), alt-text and tag editor, delete confirmation.
- **Taxonomy** — category tree with parent/child drag-and-drop; flat tag list.
- **Users** — invite user, assign/revoke role per site, deactivate.
- **Tenant Settings** — display name, locales, timezone, AI toggle, logo upload.
- Shared API client layer (`src/api/`) auto-generated from OpenAPI spec using `openapi-typescript-codegen`.
- React Router v6 with lazy-loaded routes.
- Global error boundary → maps API Problem Details to toast notifications.
- Responsive layout (sidebar nav, mobile hamburger).
- Storybook for UI component catalogue.
- Vitest + React Testing Library unit tests for all form components and API hooks (≥ 80% coverage).
- Playwright E2E smoke tests: login → create content type → create entry → publish.

Security items:
- JWT stored in `sessionStorage` (not `localStorage`) to limit XSS exposure.
- CSRF not applicable (SPA + Bearer token, no cookies).
- All API calls go through the Axios interceptor; token never logged or exposed in URLs.
- Content Security Policy header set in `MicroCMS.Admin.WebHost` (`script-src 'self'`).
- Logout clears `sessionStorage` and invalidates the token server-side (Sprint 7 identity layer will add revocation).

---

## Phase 3 — Identity, Auth & Media (Sprints 7–8)

### Sprint 7 — Identity & OAuth2
**Goal:** Full OAuth2/OIDC authentication and role-based authorization in place.

Deliverables:
- Duende IdentityServer integration (or external OIDC provider passthrough).
- User registration, login, refresh, revoke flows.
- Role management API (assign/revoke per tenant).
- `AuthorizationPolicies` constants class (all policies declared in one place).
- Permission-based authorization for content operations (Author/Editor/Approver/Publisher).
- API Keys for server-to-server (`/api/v1/apikeys` endpoint).
- Schema-per-tenant `IMigrationRunner` on onboarding (deferred from Sprint 5).
- Admin UI updated: replace mock JWT login with real OIDC PKCE flow.

Security items:
- PKCE required for all authorization code flows.
- Refresh tokens rotated on each use; rotation family stored in DB for replay detection.
- API key hashed (SHA-256) at rest; never logged.
- Brute-force lockout on login (`ILoginAttemptService`).

### Sprint 8 — Media Library
**Goal:** File upload pipeline with virus scanning, image transforms, and multi-provider storage.

Deliverables:
- Streaming multipart upload endpoint (up to 2 GB).
- `IStorageProvider` implementations: Filesystem, S3, Azure Blob.
- Virus scan integration via `IClamAvScanner` (TCP client to ClamAV daemon).
- Image variant pipeline: on-demand resize/crop/format-convert using ImageSharp.
- Signed URL generation for private assets.
- Folder hierarchy API.
- Bulk operations (move, delete, retag).
- Integration tests with Testcontainers (MinIO for S3-compatible).
- Admin UI Media Library updated to use signed URLs and show scan status badge.

Security items:
- MIME type sniffing on upload (read magic bytes, not just extension).
- Virus scan result mandatory before asset becomes `Available`; quarantined on failure.
- Signed URLs include tenant scope in HMAC payload; cross-tenant URL reuse rejected.

---

## Phase 4 — Search, Caching & GraphQL (Sprints 9–11)

### Sprint 9 — Search and Cache
**Goal:** Full-text and faceted search operational; two-tier cache cutting DB load.

Deliverables:
- `ISearchService` + `SearchEntryDocument` / `SearchRequest` / `SearchResults` DTOs (Application layer). ✅
- `OpenSearchService` adapter (Infrastructure) — tenant-partitioned alias `entries-{tenantId}`. ✅
- `NullSearchService` fallback so the app boots without an OpenSearch cluster. ✅
- `SearchEntriesQuery` + handler (`/api/v1/search` via `SearchController`). ✅
- `EntrySearchIndexerEventHandler` — indexes on publish/update, removes on unpublish/archive. ✅
- `DomainEventNotification<T>` wrapper — lets MediatR dispatch pure domain events without Domain depending on MediatR. ✅
- Two-tier cache: L1 `IMemoryCache` (process-local) + optional L2 Redis via `TwoTierCacheService`. ✅
- Cache invalidation by tag (in-memory tag→key index). ✅
- `CacheKeys` / `CacheTags` helpers — every key includes the tenantId. ✅
- Provider-driven DI: `Cache:Provider` = `None` | `Redis`, `Search:Provider` = `None` | `OpenSearch`. ✅
- Cache-aside pattern in read query handlers. ✅
- Redis integration tests using Testcontainers. ✅
- Admin UI global search bar wired to `/api/v1/search`. ✅

Security items:
- Search queries tenant-scoped in OpenSearch index alias; cross-tenant queries blocked. ✅
- Cache keys include tenant ID to prevent cross-tenant cache poisoning. ✅

### Sprint 10 — GraphQL API
**Goal:** Hot Chocolate GraphQL endpoint with dynamic schema auto-generated from content types.

Deliverables:
- Dynamic schema builder: generates GraphQL types from `ContentType` field definitions at runtime.
- Resolvers for `entry`, `entries`, `media`, `taxonomy` queries.
- Mutations for content CRUD (mirrors REST API).
- Subscriptions for `entryPublished` events via WebSocket.
- DataLoader for N+1 resolution.
- Persisted queries support.
- GraphQL-specific contract tests.

Security items:
- Query depth and complexity limits enforced (Hot Chocolate built-in).
- Introspection disabled in production unless `SystemAdmin` role present.
- Same JWT bearer + tenant-scoped authorization as REST.

### Sprint 11 — Headless Starter & TypeScript SDK
**Goal:** First-class headless consumer experience — TypeScript SDK and a production-ready Next.js starter template consuming the GraphQL API.

Deliverables:
- **`@microcms/sdk`** TypeScript package (`packages/sdk/`):
  - `MicroCmsClient` class — wraps REST and GraphQL endpoints.
  - Auto-generated types from OpenAPI spec (`openapi-typescript`).
  - Typed GraphQL query builder for content-type-specific queries.
  - `useEntry`, `useEntries`, `useMedia`, `useSearch` React hooks (TanStack Query wrappers).
  - `getEntry`, `getEntries` server-side fetch helpers (for Next.js SSR/ISR).
  - Full JSDoc + TypeScript declaration files.
  - 100% unit test coverage (Vitest).
  - Published to npm as `@microcms/sdk`.
- **`microcms-nextjs-starter`** Next.js 14 app (`packages/nextjs-starter/`):
  - App Router with ISR (`revalidate`) for published entries.
  - Dynamic routes: `[contentType]/[slug]` rendered from entry `FieldsJson`.
  - Image optimisation via `next/image` with signed URL support.
  - Tailwind CSS styling; dark mode toggle.
  - Sitemap and robots.txt auto-generated from published entries.
  - Preview mode wired to draft entries via `X-Preview-Token` header.
  - Lighthouse score ≥ 95 (performance, accessibility, SEO).
- Live demo deployment config (Vercel `vercel.json`).
- `README.md` quickstart: "from `onboard` API call to live website in 5 minutes".
- Playwright E2E tests: homepage → entry page → search → 404 handling.

Security items:
- SDK uses read-only API key (from Sprint 7) for public content; never exposes admin JWT.
- Preview token scoped to tenant + short TTL (15 min); stored server-side in encrypted cookie.
- `next/headers` CSP policy set; no inline scripts.

---

## Phase 5 — Webhooks, Events & Plugin System (Sprints 12–13)

### Sprint 12 — Webhooks and Outbox
**Goal:** Reliable event delivery to external consumers with at-least-once guarantee.

Deliverables:
- Outbox table + `OutboxDispatcher` background worker (Quartz.NET job).
- Outbox `TenantId` validation on dispatch (deferred from Sprint 5).
- Webhook subscription management API (`/api/v1/webhooks/subscriptions`).
- `WebhookDispatcher` service: HTTP POST with HMAC-SHA256 signature header.
- Retry policy with exponential back-off (Polly).
- Dead-letter queue for failed deliveries (stored in DB, admin API to replay).
- Webhook events for all significant domain events.
- Admin UI: webhook subscription CRUD screen.

Security items:
- Webhook payloads signed with `X-MicroCMS-Signature: sha256=<hmac>` header.
- Subscriber endpoints must use HTTPS.
- SSRF protection: destination URL validated against private IP blocklist.

### Sprint 13 — Plugin System
**Goal:** Plugins load/unload without restart; capability gate prevents privilege escalation.

Deliverables:
- `PluginLoader` using `AssemblyLoadContext` (isolated per plugin, unloadable).
- `PluginManifest` JSON deserialization and validation (version range, capability list).
- `CapabilityGate` service: grants only declared capabilities.
- Plugin registry API (`/api/v1/admin/plugins`).
- Plugin enable/disable per tenant.
- Sample plugin: `AuditLogPlugin` demonstrating the full contract.
- Plugin sandbox tests (assert that a plugin cannot access infrastructure beyond its grants).
- Admin UI: plugin marketplace screen (list, enable/disable, manifest viewer).

Security items:
- Assembly signature verification (strong name or Authenticode) before load.
- Plugins run in their own `AssemblyLoadContext`.
- Capability grant stored in DB per tenant; runtime capability check on every plugin call.

---

## Phase 6 — AI Module (Sprints 14–15)

### Sprint 14 — AI Core + Provider Adapters
**Goal:** All four AI interfaces operational with at least AzureOpenAI and Ollama adapters; budget enforcement live.

Deliverables:
- `AiOrchestrator`: routes calls to the tenant's configured provider(s).
- `ProviderRegistry`: maps provider names to `IAiCompletionProvider` implementations; region-aware.
- `BudgetService`: tracks token usage per tenant/user; returns `429` when limit exceeded.
- `PiiRedactor`: strips PII from prompts before dispatch; regex + NER-based.
- `PromptLibrary`: CRUD for system and tenant-level prompts with semantic versioning.
- `StructuredOutputValidator`: validates AI response against JSON Schema; repair loop (max 2 retries).
- Concrete adapters: `AzureOpenAICompletionProvider`, `OllamaCompletionProvider`.
- AI feature handlers: draft generation, rewrite, summarization, SEO assistance, translation.
- Admin UI: AI settings screen (provider config, budget limits, prompt library CRUD).
- Application unit tests for orchestrator and budget service.

Security items:
- Provider registry blocks calls to non-compliant regions per tenant's data-residency policy.
- Prompt injection detection on user-supplied content.
- All AI calls and responses audit-logged; PII redacted in logs.

### Sprint 15 — RAG, Semantic Search & AI Safety
**Goal:** Vector search and copilot features operational; safety pipeline enforced end-to-end.

Deliverables:
- Vector indexing pipeline: on entry save, generate embedding and upsert to vector store.
- `VectorSearchService`: hybrid search (BM25 + vector), result fusion.
- RAG pipeline: retrieve → assemble context → complete → cite sources.
- AI copilot endpoint (`/api/v1/ai/copilot/chat`).
- `SafetyPipeline`: pre-call moderation → call → post-call safety classifier.
- Grounded-only mode: copilot declines if no retrieved source supports claim.
- Related content suggestions.
- Media intelligence: alt-text, tag suggestions via vision model.
- Feedback endpoint: thumbs-up/down + comment on AI output.
- PgVector and Qdrant vector store adapters.
- Admin UI: copilot chat panel embedded in entry editor; AI usage analytics dashboard.
- SDK updated: `useCopilot` hook and `generateDraft` helper for Next.js starter.

Security items:
- Retrieved content from tenant index only; no cross-tenant vector leakage.
- Jailbreak detector on user messages in copilot.
- AI audit log retention ≥ 90 days; separate table, separate backup policy.

---

## Phase 7 — Observability, Performance & GA (Sprint 16)

### Sprint 16 — Observability, Hardening & GA Readiness
**Goal:** All NFRs met; system observable, performant, and security-audited.

Deliverables:
- Serilog structured logging with OTLP exporter wired to all services.
- OpenTelemetry traces: ASP.NET Core, EF Core, Redis, HTTP clients, Quartz.
- Prometheus-compatible metrics endpoint (`/metrics`).
- Health checks: `/health/live`, `/health/ready` (DB, Redis, search).
- Performance profiling: achieve P95 latency targets from §2.3.
- Load test script (k6) for 1000 concurrent users.
- OWASP ZAP scan run; all critical findings resolved.
- Dependency vulnerability scan (`dotnet list package --vulnerable`).
- E2E test suite covering all happy paths and key error paths.
- Docker multi-stage build (distroless runtime image + separate Admin UI image).
- Helm chart with resource limits, liveness/readiness probes, HPA config.
- GitHub Actions CI/CD pipeline: build → unit test → integration test → coverage → architecture test → SAST → container build → push.
- Admin UI: production build optimisation (bundle analysis, lazy loading audit).
- SDK: final API freeze, semver tag, npm publish workflow.

Security items:
- Final review of all `[Authorize]` policies — no endpoint without explicit policy.
- Secrets rotation runbook documented.
- Third-party dependency licenses audited (backend + frontend).
- Security headers verified with securityheaders.com / OWASP guidance.

---

## Sprint Progress Tracker

| Sprint | Phase | Title | Status | Completed |
|--------|-------|-------|--------|-----------|
| 0 | Foundation | Project Scaffolding | ✅ Done | 2026-04-21 |
| 1 | Foundation | Core Domain Model | ✅ Done | 2026-04-21 |
| 2 | Infrastructure & REST API | Persistence & EF Core | ✅ Done | 2026-04-22 |
| 3 | Infrastructure & REST API | Application CQRS | ✅ Done | 2026-04-22 |
| 4 | Infrastructure & REST API | REST API + Swagger | ✅ Done | 2026-04-22 |
| 5 | Multi-Tenancy & Admin UI | Multi-Tenancy Hardening | ✅ Done | 2026-04-22 |
| 6 | Multi-Tenancy & Admin UI | Admin UI (React) | ✅ Done | 2026-04-22 |
| 7 | Identity, Auth & Media | Identity & OAuth2 | ✅ Done | 2026-04-22 |
| 8 | Identity, Auth & Media | Media Library | ✅ Done | 2026-04-22 |
| 9 | Search, Caching & GraphQL | Search and Cache | ✅ Done | 2026-04-23 |
| 10 | Search, Caching & GraphQL | GraphQL API | 🔲 Not started | — |
| 11 | Search, Caching & GraphQL | Headless Starter & TypeScript SDK | 🔲 Not started | — |
| 12 | Webhooks, Events & Plugins | Webhooks and Outbox | 🔲 Not started | — |
| 13 | Webhooks, Events & Plugins | Plugin System | 🔲 Not started | — |
| 14 | AI Module | AI Core + Provider Adapters | 🔲 Not started | — |
| 15 | AI Module | RAG, Semantic Search & AI Safety | 🔲 Not started | — |
| 16 | Observability & GA | Observability, Hardening & GA | 🔲 Not started | — |

---

## Test Coverage Summary by Sprint

| Sprint | Test Projects Active | Coverage Target | Status |
|--------|----------------------|-----------------|--------|
| 0 | Architecture.Tests | Stub rules only | ✅ 5 tests |
| 1 | Domain.UnitTests | ≥ 80% Domain | ✅ 127 tests |
| 2 | Infrastructure.IntegrationTests | ≥ 80% Infrastructure | ✅ 9 tests (Docker required) |
| 3 | Application.UnitTests | ≥ 80% Application | ✅ 57 tests |
| 4 | Api.ContractTests | ≥ 80% API layer | ✅ 11 tests |
| 5 | Infrastructure.IntegrationTests (adversarial) | Cross-tenant isolation | ✅ 3 new tests (Docker required) |
| 8 | Infrastructure.IntegrationTests (MinIO) | Storage provider round-trip | ✅ 9 new tests (Docker required) |
| 8 | Application.UnitTests (Media) | Upload, bulk ops, folder CRUD | ✅ 14 new tests |
| 9 | Infrastructure.IntegrationTests (Redis) | Cache round-trip, tag invalidation, TTL, L2→L1 hydration | ✅ 9 tests (Docker required) |
| 9 | Vitest (Admin UI search) | GlobalSearchBar component + API hook | ✅ 9 tests |
| 10 | Not yet started | — | 🔲 |
| 11 | Vitest (SDK unit tests) | 100% SDK coverage | 🔲 |
| 11 | Playwright (Headless starter E2E) | Homepage → entry → search | 🔲 |
| 14 | Application.UnitTests (AI) | ≥ 80% Ai.Core | 🔲 |
| 16 | E2E.Tests | All happy paths | 🔲 |

---

## Known Deferred Items

| Item | Deferred To | Reason |
|------|-------------|--------|
| Schema-per-tenant `IMigrationRunner` | Sprint 7 | Requires identity layer for per-tenant credentials |
| Outbox `TenantId` validation on dispatch | Sprint 12 | Dispatcher not yet implemented |
| Real virus-scan pipeline (ClamAV) for `MediaAsset` | ~~Sprint 8~~ ✅ Done | `MediaScanJob` + `ClamAvScanner` TCP client implemented |
| Token revocation on Admin UI logout | Sprint 7 | Requires identity layer revocation endpoint |

---

## Definition of Done (every sprint)

1. All code compiles — `dotnet build` returns exit code 0 with zero warnings treated as errors.
2. Architecture tests pass — no layering rule violations.
3. Unit/integration tests pass — no test failures.
4. Coverage gate met for layers touched in the sprint.
5. No new SonarAnalyzer `error`-level findings introduced.
6. PR reviewed by at least one other team member.
7. `DEVELOPMENT_PLAN.md` updated if scope changed.
