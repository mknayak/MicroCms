# MicroCMS — Detailed AI Integration Plan

> **Status:** Living document. Reviewed against codebase as of Sprint 14.
> **Provider:** OpenAI only (GPT-4o for completions, `text-embedding-3-small` for embeddings).
> **Foundation already built:** `AiOrchestrator`, `BudgetService`, `PiiRedactor`, `PromptLibrary`,
> `StructuredOutputValidator`, `OpenAICompletionProvider`, `OpenAIEmbeddingProvider`,
> `CopilotConversation` aggregate (with citations + tool role), `IVectorStore` interface,
> all writing-assist command handlers, `CopilotConversations` / `CopilotMessages` /
> `CopilotMessageCitations` DB tables.

---

## 1. Vision

MicroCMS AI should feel like a **native content team member**, not a bolted-on chatbot. It operates
at two levels:

| Level | Experience | Scope | Interaction Model |
|---|---|---|---|
| **Inline Assist** | ✦ button on every field/asset | Single field or asset | Click → modal → preview → apply |
| **Copilot** | Dedicated chat page | Whole site content corpus | Conversational, RAG-grounded, tool-calling |

These two experiences share the same underlying AI stack (OpenAI, `AiOrchestrator`, `BudgetService`,
`PiiRedactor`) but differ in persistence, retrieval, and interaction model.

---

## 2. Architecture Overview

### 2.1 Layers

```
┌─────────────────────────────────────────────────────────────────┐
│  Admin.WebHost (React)                                          │
│  ├── AiAssistModal / useAiAssist    (Inline Assist UI)          │
│  └── CopilotPage / useCopilot      (Copilot Chat UI)           │
├─────────────────────────────────────────────────────────────────┤
│  API Layer  (ASP.NET Core)                                      │
│  ├── AiWritingController           /entries/{id}/draft|rewrite… │
│  ├── AiController                  /ai/drafts/generate          │
│  └── CopilotController  (new)      /copilot/conversations/…     │
├─────────────────────────────────────────────────────────────────┤
│  Application Layer  (MediatR)                                   │
│  ├── WritingAssist handlers        (exist)                      │
│  ├── GenerateDraftCommandHandler   (exists)                     │
│  ├── TranslateEntryLocaleCommand   (exists)                     │
│  ├── GenerateAltTextCommand        (exists)                     │
│  ├── CopilotChatCommand            (new)                        │
│  ├── VectorIndexEntryCommand       (new)                        │
│  └── CopilotTool handlers          (new)                        │
├─────────────────────────────────────────────────────────────────┤
│  AI Core  (MicroCMS.Ai.Core)                                    │
│  ├── AiOrchestrator                (complete/stream/embed)      │
│  ├── BudgetService                 (needs Redis persistence)    │
│  ├── PiiRedactor                   (exists)                     │
│  ├── PromptLibrary                 (exists)                     │
│  ├── StructuredOutputValidator     (exists)                     │
│  ├── RagService                    (new)                        │
│  ├── ToolRegistry                  (new)                        │
│  └── SafetyPipeline                (new, lightweight)           │
├─────────────────────────────────────────────────────────────────┤
│  Infrastructure                                                 │
│  ├── OpenAICompletionProvider      (exists)                     │
│  ├── OpenAIEmbeddingProvider       (exists)                     │
│  ├── PgVectorStore        (new — same Postgres, no extra infra) │
│  ├── EntryIndexingBackgroundService (new — on publish event)    │
│  ├── LlmServiceAdapter             (exists)                     │
│  └── AiUsageRepository    (new — replace in-memory BudgetService│
└─────────────────────────────────────────────────────────────────┘
```

### 2.2 What Exists vs What Needs Building

| Component | Status | Notes |
|---|---|---|
| `AiOrchestrator` (complete + stream + embed) | ✅ Done | Supports all three call types |
| `OpenAICompletionProvider` | ✅ Done | GPT-4o |
| `OpenAIEmbeddingProvider` | ✅ Done | `text-embedding-3-small` |
| `BudgetService` | ⚠️ Partial | In-memory only — needs DB/Redis persistence |
| `PiiRedactor`, `PromptLibrary`, `StructuredOutputValidator` | ✅ Done | |
| Writing-assist handlers (draft/rewrite/tone/summarize/translate/alt-text) | ✅ Done | |
| `CopilotConversation` aggregate + EF tables | ✅ Done | Citations, tool role modeled |
| `IVectorStore` interface | ✅ Done | |
| `PgVectorStore` implementation | ❌ Empty project | Highest priority gap |
| RAG service (retrieve + assemble context) | ❌ Missing | |
| Entry indexing pipeline | ❌ Missing | |
| `CopilotChatCommand` handler | ❌ Missing | |
| Tool registry (function calling) | ❌ Missing | |
| `CopilotController` (HTTP + SSE) | ❌ Missing | |
| Admin Copilot UI (React) | ⚠️ Design only | HTML mockup exists |
| Inline Assist UI wiring (`AiAssistModal`) | ⚠️ Partial | RichText done; others dead |
| Budget persistence (DB/Redis) | ❌ Missing | In-memory resets on restart |
| AI audit log | ❌ Missing | |
| Secret redaction in settings API responses | ❌ Missing | |

---

## 3. Two Experiences in Detail

### 3.1 Experience A — Inline AI Assist

**What it is:** Point-in-time, single-field assistance triggered by ✦ buttons throughout the editor.
No conversation history. No RAG. Fast (single LLM call).

**Surfaces:**

```
EntryEditorPage
├── ShortText fields         → "Generate with AI"  (currently dead)
├── LongText fields          → "Generate with AI"  (currently dead)
├── RichText fields          → "AI Assist"         ✅ wired (Sprint 14)
└── AiAuthoringPanel sidebar → 5 action buttons    (currently dead)
    ├── Generate Draft
    ├── Rewrite / Tone
    ├── Summarize
    ├── SEO Suggestions
    └── Translate

AssetDetail
└── Alt Text "Generate"      → (currently a plain <span>, no onClick)
```

**Shared component architecture (Sprint 15):**

```
src/components/ai/
├── AiAssistModal.tsx        ← single modal, context-typed, shared by all surfaces
├── useAiAssist.ts           ← hook: useMutation + open/close + preview state
└── AiBudgetBanner.tsx       ← shown when aiEnabled=false or last call = 429
```

`AiAssistContext` discriminated union:
```typescript
type AiAssistContext =
  | { type: 'field';  entryId: string; fieldHandle: string; fieldType: 'ShortText'|'LongText'|'RichText' }
  | { type: 'entry';  entryId: string; contentTypeId: string }
  | { type: 'asset';  assetId: string; isImage: boolean };
```

Modal tabs driven by context type:
- `field` → Draft · Rewrite · Tone · Summarize
- `entry` → Generate Draft (structured, fills multiple fields, diff view before apply)
- `asset` → Generate Alt Text · Suggest Tags

**API calls (all endpoints exist, only alt-text controller action missing):**
- Field: `POST /entries/{id}/draft|rewrite|tone`, `GET /entries/{id}/summarize`
- Entry: `POST /ai/drafts/generate` (contentTypeId + prompt → structured JSON)
- Asset: `POST /media/{assetId}/generate-alt-text` ← **missing controller action** (command exists)

---

### 3.2 Experience B — AI Copilot

**What it is:** A full conversational assistant grounded in the site's published content corpus.
The user chats naturally; the copilot retrieves relevant content, assembles context, calls OpenAI,
returns a grounded response with citations, and can invoke tools (create entry, schedule, tag
media) with an explicit user-confirmation step.

**Key design decisions:**
1. **Always tenant-scoped.** Vector search always filters by `tenantId`. Cross-tenant leakage is
   impossible by construction.
2. **Grounded-only mode.** When `GroundedOnlyMode = true` on the conversation, the copilot declines
   to respond if the RAG retrieval step returns zero results above the similarity threshold.
3. **Tools require explicit confirmation.** Any tool that mutates data (create entry, publish,
   schedule) must be confirmed by the user before execution. Read-only tools (search) execute
   immediately. This matches the design mockup.
4. **Streaming.** The copilot streams tokens via Server-Sent Events (SSE) so the response feels
   live. Non-streaming is available as fallback.
5. **Conversation persistence.** The `CopilotConversation` aggregate (already in DB) holds the
   full message history, token counts, and cost. History panel in UI = query by userId.

---

## 4. RAG System Design

RAG (Retrieval-Augmented Generation) is what makes the Copilot useful — it grounds answers in
the site's own published content rather than hallucinating.

### 4.1 What Gets Indexed

| Source | Fields to extract | Granularity | Priority |
|---|---|---|---|
| Published `Entry` | All text fields (ShortText, LongText, RichText stripped to plain text), slug, contentType, locale | Field-level chunks | P0 |
| `Page` titles and SEO meta | title, metaDescription, slug | Entry-level | P1 |
| `MediaAsset` alt-text | altText, tags, fileName | Entry-level | P1 |
| `Category` / `Tag` | name, description | Entry-level | P2 |

**Chunking strategy:**
- Each `FieldDefinition` value becomes one `VectorDocument` (field-level granularity).
- Document `Id` = `{entryId}:{fieldHandle}:{locale}` — enables precise deletion on entry update.
- Metadata stored with each chunk: `entryId`, `slug`, `contentTypeHandle`, `fieldHandle`,
  `locale`, `status`, `publishedAt` — used for filter facets in retrieval.
- Long fields (RichText > 1000 tokens): split into overlapping 512-token windows with 10% overlap.

### 4.2 Indexing Pipeline

```
Entry.Published domain event (already raised on entry.Publish())
        │
        ▼
EntryIndexingEventHandler   (new — infrastructure, subscribes to domain event)
        │
        ▼
VectorIndexEntryCommand     (new — application layer)
  1. Load entry + content type schema
  2. Extract text fields → strip HTML → build VectorDocument list
  3. Embed batch via AiOrchestrator.EmbedAsync (batched, max 100 docs/call)
  4. Upsert to IVectorStore (tenantId-scoped)
        │
        ▼
PgVectorStore.UpsertAsync   (new — infrastructure)
```

**Re-indexing triggers:**
- Entry published → index
- Entry unpublished / archived → delete from index
- Entry fields updated AND status = Published → re-index (delete old, upsert new)
- Content type field added/removed → full re-index of that content type (background job)

**Background re-index job:** `ReIndexSiteCommand` — rebuilds all published entries for a site.
Exposed as admin action in Site Settings → AI tab.

### 4.3 Vector Store — PgVector (Primary)

**Why PgVector:** Same PostgreSQL instance, zero additional infrastructure, sufficient for millions
of entries. Can add Qdrant/OpenSearch later if scale demands it.

**Schema:**
```sql
-- Migration: AddPgVectorExtension
CREATE EXTENSION IF NOT EXISTS vector;

CREATE TABLE "ContentVectors" (
    "Id"              TEXT         NOT NULL,          -- {entryId}:{fieldHandle}:{locale}
    "TenantId"        UUID         NOT NULL,
    "EntryId"         UUID         NOT NULL,
    "FieldHandle"     TEXT         NOT NULL,
    "Locale"          TEXT         NOT NULL,
    "ContentTypeHandle" TEXT       NOT NULL,
    "Content"         TEXT         NOT NULL,          -- plain text (for BM25 fallback)
    "Embedding"       VECTOR(1536) NOT NULL,          -- text-embedding-3-small = 1536 dims
    "Metadata"        JSONB        NOT NULL DEFAULT '{}',
    "IndexedAt"       TIMESTAMPTZ  NOT NULL,
    CONSTRAINT "PK_ContentVectors" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_ContentVectors_Tenants" FOREIGN KEY ("TenantId")
        REFERENCES "Tenants"("Id") ON DELETE CASCADE
);

-- HNSW index for fast ANN search, tenant-filtered
CREATE INDEX "IX_ContentVectors_TenantId_Embedding"
    ON "ContentVectors" USING hnsw ("TenantId", "Embedding" vector_cosine_ops)
    WITH (m = 16, ef_construction = 64);

-- Full-text index for BM25/hybrid
CREATE INDEX "IX_ContentVectors_TenantId_Content_FTS"
    ON "ContentVectors" USING gin ("TenantId", to_tsvector('english', "Content"));
```

### 4.4 Retrieval — Hybrid Search

Pure vector search misses exact keyword matches (e.g. product names, API endpoints). Hybrid
combines vector cosine similarity with PostgreSQL BM25 full-text search.

```
Query text
    │
    ├──► Embed query → query vector (1536 dims)
    │         │
    │         ▼
    │    IVectorStore.SearchAsync(tenantId, queryVector, topK=20, minScore=0.70)
    │         → vector candidates with cosine scores
    │
    └──► tsvector BM25 search (same Postgres)
              → keyword candidates with rank scores
                   │
                   ▼
         Result Fusion (Reciprocal Rank Fusion)
              → merged list, top 5 by combined score
                   │
                   ▼
         Context Assembly
              → [1] chunk · [2] chunk · … · [5] chunk
              → injected into system prompt as cited sources
```

**`RagService`** (new, `MicroCMS.Ai.Core`):
```csharp
public sealed class RagService
{
    Task<RagContext> RetrieveAsync(
        TenantId tenantId,
        string query,
        RagRetrievalOptions options,   // topK, minScore, localeFilter, contentTypeFilter
        CancellationToken ct);
}

public sealed record RagContext(
    IReadOnlyList<RetrievedChunk> Chunks,   // ordered by fused score
    string AssembledContextBlock);           // pre-formatted for insertion into system prompt

public sealed record RetrievedChunk(
    string DocumentId,
    Guid EntryId,
    string Slug,
    string Title,
    string Content,
    double Score,
    string ContentTypeHandle,
    string FieldHandle);
```

### 4.5 Context Window Assembly

The retrieved chunks are assembled into a system-prompt block:

```
You are MicroCMS Copilot. You help content authors write, edit, and manage content.
Answer questions using ONLY the sources below. If none of the sources support your answer,
say "I don't have enough information in the current content to answer that."

--- SOURCES ---
[1] Title: "REST API Design — Rate Limiting" (slug: api-design/rate-limiting)
    "Per-tenant and per-API-client buckets using token-bucket limiter. Authenticated user: 600 rpm…"

[2] Title: "Rate Limits — FAQ"
    "Q: What happens when I hit the rate limit? A: MicroCMS returns HTTP 429 with a Retry-After header…"
--- END SOURCES ---
```

Each `[N]` citation number maps to a `CopilotCitation` stored on the assistant `CopilotMessage`.

---

## 5. Copilot Chat Design

### 5.1 Conversation Loop (per message)

```
POST /api/v1/copilot/conversations/{id}/messages
  body: { content: "Write a blog intro about rate limits…" }
        │
        ▼
CopilotChatCommand handler
  1. Load CopilotConversation aggregate (or create new)
  2. Validate: message count < 200, budget not exceeded
  3. PiiRedactor.RedactAsync(userMessage)
  4. RagService.RetrieveAsync(tenantId, userMessage, options)
     → assembledContextBlock + chunks
  5. Build OpenAI messages array:
     [system: copilot prompt + context block]
     [history: last N messages from conversation]
     [user: redacted message]
  6. If tools are enabled → attach tool definitions (function schemas)
  7. AiOrchestrator.StreamAsync(request)  ← SSE path
     — OR — AiOrchestrator.CompleteAsync  ← non-streaming path
  8. If response contains tool_calls:
     → Return ToolCallPendingResult (no mutation yet — user must confirm)
     → Store as CopilotMessage with role = Tool and status = PendingConfirmation
  9. Else:
     → conversation.AddAssistantMessage(content, tokens, cost, citations)
     → Save conversation
 10. BudgetService.RecordUsageAsync
 11. (future) AiAuditLog.AppendAsync
```

### 5.2 Tool Calling

Tools = OpenAI function-calling schemas + MediatR command handlers.

**Tool definitions sent to OpenAI:**
```json
[
  {
    "name": "create_entry",
    "description": "Creates a new Draft entry in the CMS. Requires user confirmation.",
    "parameters": {
      "type": "object",
      "properties": {
        "contentTypeId": { "type": "string", "format": "uuid" },
        "slug":          { "type": "string" },
        "locale":        { "type": "string" },
        "fields":        { "type": "object" }
      },
      "required": ["contentTypeId", "slug", "locale"]
    }
  },
  {
    "name": "search_entries",
    "description": "Searches published entries by keyword. Returns immediately without confirmation.",
    "parameters": { … }
  },
  {
    "name": "schedule_publish",
    "description": "Schedules an entry to publish at a specified time. Requires user confirmation.",
    "parameters": { … }
  },
  {
    "name": "tag_media",
    "description": "Sets tags on a media asset. Requires user confirmation.",
    "parameters": { … }
  }
]
```

**Confirmation flow (matches design mockup exactly):**
1. OpenAI returns `tool_calls` → copilot renders a confirmation card (not executed yet).
2. User clicks "Confirm" → `POST /copilot/conversations/{id}/tools/{callId}/confirm`
3. Server dispatches the corresponding MediatR command.
4. Result is appended as a Tool-role message.
5. User clicks "Cancel" → tool call is discarded; conversation continues.

**`ToolRegistry`** (new, `MicroCMS.Ai.Core`):
```csharp
public sealed class ToolRegistry
{
    IReadOnlyList<ToolDefinition> GetEnabledTools(SiteId siteId);   // reads from settings
    bool RequiresConfirmation(string toolName);                      // mutating tools = true
    Task<ToolResult> ExecuteAsync(string toolName, JsonElement args,
        TenantId tenantId, Guid userId, CancellationToken ct);
}
```

Tools enabled per-site via `AiSettingKeys.EnabledCopilotTools` (comma-separated list).

### 5.3 Controller — HTTP + SSE

```csharp
[Route("api/v{version:apiVersion}/copilot")]
public sealed class CopilotController : ApiControllerBase
{
    // Start or list conversations
    GET    /conversations                           → PagedResult<ConversationSummaryDto>
    POST   /conversations                           → ConversationDto (new conversation)

    // Chat
    POST   /conversations/{id}/messages             → MessageDto (non-streaming)
    GET    /conversations/{id}/messages/stream      → text/event-stream (SSE)
    GET    /conversations/{id}/messages             → IReadOnlyList<MessageDto>

    // Tool confirmation
    POST   /conversations/{id}/tools/{callId}/confirm  → ToolResultDto
    DELETE /conversations/{id}/tools/{callId}           → (cancel tool)

    // Management
    DELETE /conversations/{id}                      → 204
    PATCH  /conversations/{id}/mode                 → toggle GroundedOnlyMode
}
```

### 5.4 Streaming (SSE)

```
GET /conversations/{id}/messages/stream
    Accept: text/event-stream
    ?prompt=Write+a+blog+post…

Server sends:
    event: delta
    data: {"delta":"Here","conversationId":"…"}

    event: delta
    data: {"delta":" is","conversationId":"…"}

    event: citation
    data: {"index":1,"entryId":"…","slug":"…","title":"…","score":0.94}

    event: done
    data: {"messageId":"…","promptTokens":420,"completionTokens":311,"costUsd":0.0042}
```

Frontend uses `EventSource` or `fetch` with `ReadableStream`. The `useCopilot` hook manages
the stream, accumulating deltas and citations for display.

### 5.5 Grounded-Only Mode

When `GroundedOnlyMode = true`:
- If `RagService.RetrieveAsync` returns 0 chunks above `minScore` → copilot returns a
  standard refusal message: *"I couldn't find relevant content in this site to answer that question."*
- If chunks exist but the LLM attempts to answer beyond them → post-response safety check
  compares answer to source chunks; low overlap → appends a disclaimer.

---

## 6. Content Authoring Benefits — Concrete Features

### 6.1 What Each Feature Unlocks

| Feature | User Benefit | Surfaces |
|---|---|---|
| **Field-level draft** | Author types a 1-sentence brief; AI fills the field | ShortText, LongText, RichText inline ✦ button |
| **Rewrite / Tone change** | Rewrite for different audiences without losing intent | All text fields |
| **Summarize** | Generate TL;DR for meta descriptions, abstracts | LongText, RichText |
| **SEO Suggestions** | Populate `seoTitle`, `seoDescription`, `keywords` from body content | Sidebar action |
| **Generate Draft** | From a brief prompt, fill an entire entry's fields (structured output) | Sidebar action + `AiController` |
| **Translate** | Create a new locale variant from existing content | Sidebar action, already wired |
| **Alt Text** | One-click accessible descriptions for images | AssetDetail |
| **Tag Suggestions** | Auto-suggest taxonomy tags from entry content | (future, after RAG) |
| **Copilot: Grounded Q&A** | Ask questions about the site's published content | Copilot page |
| **Copilot: Create Entry** | Describe an entry in chat; AI fills fields, user confirms, entry created | Copilot page (tool) |
| **Copilot: Related Content** | "Find entries related to this topic" | Copilot page (search tool) |
| **Copilot: Translate batch** | "Translate all draft blog posts to French" | Copilot page (tool, future) |
| **Copilot: SEO audit** | "Review SEO meta for all published entries" | Copilot page (future) |

### 6.2 Content Quality Checks (passive, no LLM call needed)

The `QualityChecksPanel` in `EntryEditorPage` currently has hardcoded fake data. It should be
wired to real checks — most of which are **deterministic, no AI cost**:

| Check | Implementation | AI needed? |
|---|---|---|
| Missing locale variant | Compare entry locales vs site locale list | ❌ No |
| SEO fields empty | Check `seoTitle` / `seoDescription` field values | ❌ No |
| RichText word count | Count words in field | ❌ No |
| Readability score | Flesch-Kincaid (pure algorithm) | ❌ No |
| Missing alt text on linked media | Check `MediaAsset.AltText` | ❌ No |
| Grammar check | OpenAI `gpt-4o-mini` (cheap) | ✅ Yes (optional) |
| PII in content | `PiiRedactor` patterns (already built) | ❌ No (regex) |

---

## 7. Safety & Security

### 7.1 `SafetyPipeline` (lightweight, Sprint 16)

Not a full moderation service — a thin wrapper that:
1. **Pre-call:** Runs `PiiRedactor` on user input (already built). Checks `PromptInjectionDetection`
   flag — if enabled, scans for common injection patterns (ignore previous instructions, etc.) using
   a small allowlist regex + optional OpenAI moderation endpoint.
2. **Post-call:** Validates that assistant response does not leak PII from tenant data. Optional
   readability/safety check via `IAiModerationProvider` (interface already defined).

```csharp
public sealed class SafetyPipeline
{
    Task<SafetyCheckResult> PreCallAsync(string userInput, TenantId tenantId, SiteId? siteId, CancellationToken ct);
    Task<SafetyCheckResult> PostCallAsync(string assistantOutput, TenantId tenantId, CancellationToken ct);
}
```

### 7.2 Tenant Isolation

- All vector searches include `TenantId` filter — enforced at `PgVectorStore` level, not caller level.
- `CopilotConversation` has `TenantId`; controller verifies ownership before loading.
- Tool execution dispatches MediatR commands with the authenticated user's `TenantId` claim.

### 7.3 Secret Redaction (Sprint 15 fix)

`IsSecret = true` config entries must return `"***"` in all read API responses.
The `SettingsReader` already carries the `IsSecret` flag — mapper must redact before serialization.

### 7.4 AI Audit Log (Sprint 16)

New table `AiAuditLog`:
```sql
CREATE TABLE "AiAuditLog" (
    "Id"          UUID         PRIMARY KEY,
    "TenantId"    UUID         NOT NULL,
    "UserId"      UUID         NOT NULL,
    "FeatureKey"  TEXT         NOT NULL,   -- "writing_assist", "copilot", "alt_text", etc.
    "Provider"    TEXT         NOT NULL,
    "Model"       TEXT         NOT NULL,
    "PromptHash"  TEXT         NOT NULL,   -- SHA-256 of redacted prompt (not raw)
    "PromptTokens"   INT       NOT NULL,
    "CompletionTokens" INT     NOT NULL,
    "CostUsd"     DECIMAL(18,6) NOT NULL,
    "CreatedAt"   TIMESTAMPTZ  NOT NULL
);
```
Retention: 90 days (partition by month, drop old partitions).

### 7.5 Budget Persistence (Sprint 15 fix)

`BudgetService` is currently in-memory — resets on restart, inaccurate in multi-instance deployments.
Replace with `AiUsageRepository` backed by a `AiDailyUsage` table:
```sql
CREATE TABLE "AiDailyUsage" (
    "TenantId"    UUID    NOT NULL,
    "UserId"      UUID    NOT NULL,
    "Date"        DATE    NOT NULL,
    "TotalTokens" BIGINT  NOT NULL DEFAULT 0,
    "TotalCostUsd" DECIMAL(18,6) NOT NULL DEFAULT 0,
    CONSTRAINT "PK_AiDailyUsage" PRIMARY KEY ("TenantId", "UserId", "Date")
);
```

---

## 8. Sprint Plan

### Sprint 15 — Inline AI Assist UI Wiring (Frontend-heavy)

**Goal:** Every ✦ AI button in the admin is functional. No new backend features needed except
the missing alt-text controller action and secret redaction fix.

**Backend (minimal):**
- [ ] Add `POST /media/{assetId}/generate-alt-text` controller action (command exists).
- [ ] Fix `IsSecret` redaction in settings API responses.
- [ ] Replace in-memory `BudgetService` with DB-backed `AiUsageRepository`.

**Frontend:**
- [ ] Create `src/components/ai/AiAssistModal.tsx` (shared modal, context-typed).
- [ ] Create `src/components/ai/useAiAssist.ts` (hook).
- [ ] Create `src/components/ai/AiBudgetBanner.tsx`.
- [ ] Extend `src/api/ai.ts` with `generateAltText(assetId)` and `generateDraft(contentTypeId, prompt)`.
- [ ] Wire `ShortText` / `LongText` inline "Generate with AI" buttons in `FieldInput.tsx`.
- [ ] Wire `AiAuthoringPanel` 5 sidebar buttons in `EntryEditorPage` (replace static list).
- [ ] Wire `AssetDetail` "Generate" span → proper button with `useAiAssist`.
- [ ] Fix hardcoded "Claude Sonnet" text → "OpenAI" sourced from site settings.
- [ ] Wire `QualityChecksPanel` to real deterministic checks (no AI calls needed).

---

### Sprint 16 — RAG + Vector Indexing

**Goal:** Published entries are indexed in PgVector. RAG retrieval is live and usable from
the copilot page.

- [ ] EF Core migration: `pgvector` extension + `ContentVectors` table (HNSW + FTS indexes).
- [ ] `PgVectorStore` implementation (upsert, hybrid search, delete).
- [ ] `RagService` (embed query → vector search → BM25 merge → RRF fusion → assemble context).
- [ ] `EntryIndexingEventHandler` (subscribes to `EntryPublishedEvent`, dispatches `VectorIndexEntryCommand`).
- [ ] `VectorIndexEntryCommand` handler (extract text fields → batch embed → upsert).
- [ ] `EntryUnpublishedEvent` handler → delete from vector store.
- [ ] `ReIndexSiteCommand` (background job for bulk re-index).
- [ ] Admin UI: "Re-index Content" button in Site Settings → AI tab.
- [ ] Unit tests: `RagService`, `PgVectorStore` (integration).

---

### Sprint 17 — Copilot Chat

**Goal:** Copilot page is fully functional: RAG-grounded chat, tool calling with confirmation,
streaming responses, conversation history.

**Backend:**
- [ ] `CopilotChatCommand` handler (full conversation loop: RAG → build messages → stream → persist).
- [ ] `ToolRegistry` with `create_entry`, `search_entries`, `schedule_publish`, `tag_media`.
- [ ] `CopilotController` (conversations CRUD, messages, SSE stream, tool confirm/cancel).
- [ ] Grounded-only mode enforcement in `CopilotChatCommand`.
- [ ] `SafetyPipeline` (PII pre-call + prompt injection detection).
- [ ] `AiAuditLog` table + append on every AI call.

**Frontend:**
- [ ] `CopilotPage.tsx` — wire all static elements from design mockup.
- [ ] `useCopilot` hook — `EventSource` stream handling, delta accumulation, citation rendering.
- [ ] Conversation history panel (load from `GET /copilot/conversations`).
- [ ] Tool confirmation card component.
- [ ] Budget usage display (tokens used / cap) in topbar.
- [ ] Grounded-only mode toggle.
- [ ] Suggested prompts (static list per context, no AI needed).

---

### Sprint 18 — Polish, Analytics & Quality Checks

- [ ] AI usage analytics dashboard in admin (charts: tokens/day, cost/day, by feature, by user).
- [ ] `QualityChecksPanel` — grammar check via `gpt-4o-mini` (optional, opt-in per site).
- [ ] Related content suggestions in entry editor sidebar (uses RAG, no new endpoint needed).
- [ ] Copilot "Insert into editor" — copy citation content back to entry field.
- [ ] Feedback endpoint (thumbs-up/down on copilot messages → `AiFeedback` table).
- [ ] Conversation export (markdown download).

---

## 9. Key Decisions & Principles

| Decision | Rationale |
|---|---|
| **PgVector as primary vector store** | Same Postgres instance, zero extra infra, HNSW is fast enough for millions of entries. Qdrant/OpenSearch remain in project shells for future. |
| **Field-level chunks, not entry-level** | Retrieves the exact field that's relevant, not the whole entry. Better precision. Document IDs include field handle for targeted deletion on update. |
| **Hybrid search (vector + BM25)** | Pure vector misses exact terms (API names, product codes). PostgreSQL already has `tsvector`. Reciprocal Rank Fusion merges both cheaply. |
| **Tool confirmation required for mutations** | Authors trust the AI more when they review before executing. Matches the design mockup. Read-only tools (search) are immediate. |
| **SSE over WebSocket for streaming** | SSE is simpler (HTTP), fire-and-forget, works through proxies without upgrade. No bidirectional need for copilot response streaming. |
| **Grounded-only mode is per-conversation** | Some conversations need creativity (drafting); others need factual grounding (Q&A). Togglable at conversation level. |
| **No Ollama/Azure/Anthropic adapters** | OpenAI only until usage validates the need. `IAiCompletionProvider` / `IAiEmbeddingProvider` abstractions remain — adding a provider is a single new class. |
| **`PromptLibrary` for all prompts** | No prompts are hardcoded in C#. All prompts live in `SiteSettings.ConfigEntries` and are editable by admins without a deployment. |
