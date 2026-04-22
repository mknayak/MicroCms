# MicroCMS — Development Plan

**Version:** 1.3  
**Last Updated:** 2026-04-22  
**Sprint Cadence:** 2 weeks  
**Target GA:** Sprint 14 (28 sprints = ~7 months from kickoff)  
**Current Status:** Sprint 3 complete — entering Sprint 4 (REST API + Swagger)

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
- `SpecificationEvaluator` translating `ISpecification<T>` to EF Core LINQ (criteria → includes → ordering → paging). ✅
- SQLite initial migration (`Persistence/Sqlite/Migrations/`). ✅
- PostgreSQL initial migration (`Persistence/PostgreSql/Migrations/`). ✅
- `ApplicationDbContextFactory` (design-time factory for `dotnet ef`). ✅
- `HttpContextCurrentUser` resolving `UserId`, `TenantId`, `Email`, `Roles` from JWT claims. ✅
- `SystemDateTimeProvider` (`IDateTimeProvider` production implementation). ✅
- `DependencyInjection.AddInfrastructure()` — provider-switching (SQLite/PostgreSQL), all repositories, UoW, ICurrentUser, IDateTimeProvider. ✅
- Infrastructure integration tests (`Testcontainers.PostgreSql`): CRUD round-trip, multi-tenant isolation (cross-tenant reads return zero rows), outbox atomicity. ✅

Security items:
- Parameterised queries only — no raw SQL string concatenation anywhere in Infrastructure. ✅
- Tenant filter applied globally via `HasQueryFilter` (instance-field pattern); cannot be bypassed without `IgnoreQueryFilters()` which must be paired with a `SystemAdmin` role assertion at the call site. ✅
- Cross-tenant isolation verified by integration tests (`MultiTenantIsolationTests`). ✅

### Sprint 3 — Application Layer: Content CQRS ✅ DONE
**Goal:** All content CRUD operations implemented end-to-end through Application layer.

Deliverables:
- `ICommand<T>` / `ICommand` / `IQuery<T>` marker interfaces distinguishing commands from read-only queries. ✅
- Commands: `CreateEntryCommand`, `UpdateEntryCommand`, `PublishEntryCommand`, `UnpublishEntryCommand`, `DeleteEntryCommand`, `SchedulePublishCommand`, `RollbackEntryVersionCommand`. ✅
- Queries: `GetEntryQuery`, `ListEntriesQuery` (paginated), `GetEntryVersionsQuery`. ✅
- FluentValidation validators for `CreateEntry`, `UpdateEntry`, `SchedulePublish` (slug format, JSON validity, date ordering). ✅
- `UnitOfWorkBehavior` — calls `SaveChangesAsync` after command handlers only (queries skipped). ✅
- `EntryDto`, `EntryListItemDto`, `EntryVersionDto` + Mapperly source-generated `EntryMapper`. ✅
- `EntryBySlugAndSiteSpec` for slug-uniqueness enforcement; `EntriesBySiteSpec` with paged/count overloads. ✅
- `HasPolicyAttribute`, `ContentPolicies`, `Roles`, `RolePermissions` constants. ✅
- `IApplicationAuthorizationService` + `DefaultApplicationAuthorizationService` (role→policy mapping, all-or-nothing evaluation). ✅
- `AuthorizationBehavior` — checks `[HasPolicy]` attribute; throws `MissingPolicyException` if absent (fail-secure), `UnauthorizedException` (401) or `ForbiddenException` (403) on failure. ✅
- `Application/DependencyInjection.AddApplication()` — registers MediatR with ordered pipeline, FluentValidation, authorization service. ✅
- Application unit tests: `CreateEntryCommandHandlerTests`, `UpdateEntryCommandHandlerTests`, `PublishEntryCommandHandlerTests`, `DeleteEntryCommandHandlerTests`, `GetEntryQueryHandlerTests`, `ListEntriesQueryHandlerTests`, `GetEntryVersionsQueryHandlerTests`, `AuthorizationBehaviorTests`, `RolePermissionsTests`, `DefaultApplicationAuthorizationServiceTests`, `CreateEntryCommandValidatorTests`. ✅

Implementation notes:
- MediatR pipeline order: `LoggingBehavior → AuthorizationBehavior → ValidationBehavior → UnitOfWorkBehavior → Handler`.
- Slug uniqueness is checked in the handler (not validator) to avoid requiring a repository in the validator.
- `Entry.Status` (enum) serialised to string in `EntryDto` via Mapperly's default enum-to-string mapping.
- `[Mapper(UnmappedMemberStrategy = UnmappedMemberStrategy.Ignore)]` suppresses RMG warnings for extra source props (e.g. `Versions`, `FieldsJson`) not present in narrow DTOs — required due to `TreatWarningsAsErrors`.

Security items:
- `AuthorizationBehavior` added to pipeline at position 2 (after logging, before validation) — evaluates `[HasPolicy]` attributes on the request type before the handler is invoked. ✅
- Every command/query requires at least one `[HasPolicy]` decoration; absence throws `MissingPolicyException` at runtime (fail-secure default). ✅
- `UnauthorizedException` (HTTP 401) vs `ForbiddenException` (HTTP 403) cleanly separated — maps to distinct HTTP codes in Sprint 4. ✅
- Role-to-policy mapping lives in `RolePermissions` (single source of truth); new roles only need to be added there. ✅

### Sprint 4 — REST API + Swagger
**Goal:** Full HTTP API surface for content, tenants, and media, ready for consumer integration.

Deliverables:
- `TenantsController` (CRUD + site management).
- `ContentTypesController` (define schemas).
- `EntriesController` (CRUD, publish/unpublish, version history, locale variants).
- `MediaController` (upload, list, metadata, delete, signed URL).
- `TaxonomyController` (categories, tags).
- API versioning (`/api/v1/`).
- Swagger / OpenAPI 3.0 docs auto-generated.
- Problem Details (RFC 7807) middleware — maps domain exceptions to correct HTTP codes.
- Rate limiting middleware: token-bucket per tenant (ASP.NET Core built-in).
- JWT bearer authentication + CORS configured.
- Contract tests using `Microsoft.AspNetCore.Mvc.Testing`.

Security items:
- All endpoints annotated with explicit `[Authorize(Policy)]`.
- HSTS and HTTPS redirect enforced in non-Development environments.
- Correlation ID middleware for request tracing.
- `X-Content-Type-Options: nosniff`, `X-Frame-Options: DENY`, `Referrer-Policy` headers set globally.

---

## Phase 2 — Multi-Tenancy, Auth & Media (Sprints 5–7)

### Sprint 5 — Multi-Tenancy Hardening
**Goal:** Tenant isolation tested under adversarial conditions; tenant onboarding flow complete.

Deliverables:
- Tenant resolution middleware: subdomain → TenantId lookup with LRU cache.
- Schema-per-tenant mode: `IMigrationRunner` per-tenant on onboarding.
- `TenantOnboardingService`: provision DB schema, default roles, admin user, default site.
- Per-tenant quota enforcement (storage GB, API RPM, user count) via `IQuotaService`.
- Tenant admin API (`/api/v1/admin/tenants/...`).
- Infrastructure integration tests: cross-tenant isolation (attempt to read tenant B data from tenant A session, expect 0 results).

Security items:
- Tenant resolution cannot be spoofed via `X-Tenant-Id` header unless caller has `SystemAdmin` role.
- All outbox events include `TenantId`; webhook dispatcher validates before dispatch.

### Sprint 6 — Identity & OAuth2
**Goal:** Full OAuth2/OIDC authentication and role-based authorization in place.

Deliverables:
- Duende IdentityServer integration (or external OIDC provider passthrough).
- User registration, login, refresh, revoke flows.
- Role management API (assign/revoke per tenant).
- `AuthorizationPolicies` constants class (all policies declared in one place).
- Permission-based authorization for content operations (Author/Editor/Approver/Publisher).
- API Keys for server-to-server (`/api/v1/apikeys` endpoint).

Security items:
- PKCE required for all authorization code flows.
- Refresh tokens rotated on each use; rotation family stored in DB for replay detection.
- API key hashed (SHA-256) at rest; never logged.
- Brute-force lockout on login (`ILoginAttemptService`).

### Sprint 7 — Media Library
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

Security items:
- MIME type sniffing on upload (read magic bytes, not just extension).
- Virus scan result is mandatory before asset becomes `Available`; upload is quarantined on failure.
- Signed URLs include tenant scope in HMAC payload; cross-tenant URL reuse rejected.

---

## Phase 3 — Search, Caching & GraphQL (Sprints 8–9)

### Sprint 8 — Search and Cache
**Goal:** Full-text and faceted search operational; two-tier cache cutting DB load.

Deliverables:
- `ISearchService` with OpenSearch adapter.
- Entry indexing on publish/unpublish events (via `IDomainEvent` handler).
- Full-text search endpoint (`/api/v1/search`).
- Two-tier cache: L1 `IMemoryCache` (process-local), L2 Redis.
- Cache invalidation by tag (invalidate all entries for a tenant on bulk publish).
- Cache-aside pattern in read query handlers.
- Redis integration tests using Testcontainers.

Security items:
- Search queries tenant-scoped in OpenSearch index alias; cross-tenant queries blocked.
- Cache keys include tenant ID to prevent cross-tenant cache poisoning.

### Sprint 9 — GraphQL API
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

---

## Phase 4 — Webhooks, Events & Plugin System (Sprints 10–11)

### Sprint 10 — Webhooks and Outbox
**Goal:** Reliable event delivery to external consumers with at-least-once guarantee.

Deliverables:
- Outbox table + `OutboxDispatcher` background worker (Quartz.NET job).
- Webhook subscription management API (`/api/v1/webhooks/subscriptions`).
- `WebhookDispatcher` service: HTTP POST with HMAC-SHA256 signature header.
- Retry policy with exponential back-off (Polly).
- Dead-letter queue for failed deliveries (stored in DB, admin API to replay).
- Webhook events for all significant domain events (entry published, media uploaded, tenant created, etc.).

Security items:
- Webhook payloads signed with `X-MicroCMS-Signature: sha256=<hmac>` header.
- Subscriber endpoints must use HTTPS (enforced at subscription creation time).
- SSRF protection: destination URL validated against allowlist / blocklist of private IP ranges.

### Sprint 11 — Plugin System
**Goal:** Plugins load/unload without restart; capability gate prevents privilege escalation.

Deliverables:
- `PluginLoader` using `AssemblyLoadContext` (isolated per plugin, unloadable).
- `PluginManifest` JSON deserialization and validation (version range, capability list).
- `CapabilityGate` service: grants only declared capabilities.
- Plugin registry API (`/api/v1/admin/plugins`).
- Plugin enable/disable per tenant.
- Sample plugin: `AuditLogPlugin` demonstrating the full contract.
- Plugin sandbox tests (assert that a plugin cannot access infrastructure beyond its grants).

Security items:
- Assembly signature verification (strong name or Authenticode) before load.
- Plugins run in their own `AssemblyLoadContext`; cannot access types outside their granted interfaces.
- Capability grant stored in DB per tenant; runtime capability check on every plugin call.

---

## Phase 5 — AI Module (Sprints 12–13)

### Sprint 12 — AI Core + Provider Adapters
**Goal:** All four AI interfaces operational with at least AzureOpenAI and Ollama adapters; budget enforcement live.

Deliverables:
- `AiOrchestrator`: routes calls to the tenant's configured provider(s).
- `ProviderRegistry`: maps provider names to `IAiCompletionProvider` implementations; region-aware.
- `BudgetService`: tracks token usage per tenant/user; returns `429` when limit exceeded.
- `PiiRedactor`: strips PII from prompts before dispatch; regex + NER-based.
- `PromptLibrary`: CRUD for system and tenant-level prompts with semantic versioning.
- `StructuredOutputValidator`: validates AI response against JSON Schema; runs repair loop (max 2 retries).
- Concrete adapters: `AzureOpenAICompletionProvider`, `OllamaCompletionProvider`.
- AI feature handlers: draft generation, rewrite, summarization, SEO assistance, translation.
- Application unit tests for orchestrator and budget service.

Security items:
- Provider registry blocks calls to non-compliant regions per tenant's data-residency policy.
- Prompt injection detection on user-supplied content before it enters any prompt.
- All AI calls and responses audit-logged with tenant + user scope; PII redacted in logs.

### Sprint 13 — RAG, Semantic Search & AI Safety
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

Security items:
- Retrieved content from tenant index only; no cross-tenant vector leakage.
- Jailbreak detector on user messages in copilot (prompt injection classifier).
- AI audit log retention ≥ 90 days; separate table, separate backup policy.

---

## Phase 6 — Observability, Performance & GA (Sprint 14)

### Sprint 14 — Observability, Hardening & GA Readiness
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
- Docker multi-stage build (distroless runtime image).
- Helm chart with resource limits, liveness/readiness probes, HPA config.
- GitHub Actions CI/CD pipeline: build → unit test → integration test → coverage → architecture test → SAST → container build → push.

Security items:
- Final review of all `[Authorize]` policies — no endpoint without explicit policy.
- Secrets rotation runbook documented.
- Third-party dependency licenses audited.
- Security headers verified with securityheaders.com / OWASP guidance.

---

## Sprint Progress Tracker

| Sprint | Phase | Status | Completed |
|--------|-------|--------|-----------|
| 0 | Foundation | ✅ Done | 2026-04-21 |
| 1 | Foundation | ✅ Done | 2026-04-21 |
| 2 | Infrastructure & REST API | ✅ Done | 2026-04-22 |
| 3 | Infrastructure & REST API | ✅ Done | 2026-04-22 |
| 4 | Infrastructure & REST API | 🔲 Not started | — |
| 5 | Multi-Tenancy, Auth & Media | 🔲 Not started | — |
| 6 | Multi-Tenancy, Auth & Media | 🔲 Not started | — |
| 7 | Multi-Tenancy, Auth & Media | 🔲 Not started | — |
| 8 | Search, Caching & GraphQL | 🔲 Not started | — |
| 9 | Search, Caching & GraphQL | 🔲 Not started | — |
| 10 | Webhooks, Events & Plugins | 🔲 Not started | — |
| 11 | Webhooks, Events & Plugins | 🔲 Not started | — |
| 12 | AI Module | 🔲 Not started | — |
| 13 | AI Module | 🔲 Not started | — |
| 14 | Observability & GA | 🔲 Not started | — |

---

## Test Coverage Summary by Sprint

| Sprint | New Test Projects Active | Coverage Target | Status |
|--------|--------------------------|-----------------|--------|
| 0 | Architecture.Tests | Stub rules only | ✅ |
| 1 | Domain.UnitTests | ≥ 80% Domain | ✅ |
| 2 | Infrastructure.IntegrationTests | ≥ 80% Infrastructure | ✅ |
| 3 | Application.UnitTests | ≥ 80% Application | 🔲 |
| 4 | Api.ContractTests | ≥ 80% API layer | 🔲 |
| 12 | Application.UnitTests (AI) | ≥ 80% Ai.Core | 🔲 |
| 14 | E2E.Tests | All happy paths | 🔲 |

---

## Definition of Done (every sprint)

1. All code compiles — `dotnet build` returns exit code 0 with zero warnings treated as errors.
2. Architecture tests pass — no layering rule violations.
3. Unit/integration tests pass — no test failures.
4. Coverage gate met for layers touched in the sprint.
5. No new SonarAnalyzer `error`-level findings introduced.
6. PR reviewed by at least one other team member.
7. `DEVELOPMENT_PLAN.md` updated if scope changed.
