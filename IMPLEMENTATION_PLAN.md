# MicroCMS — Design-to-Implementation Gap Analysis & Plan

**Prepared:** 2026-04-26  
**Scope:** Gap analysis between `/design/*.html` mockups and `Admin.WebHost/clientApp/src` React implementation  
**Methodology:** Full read of all 15 design HTML files and all source files in `clientApp/src`

---

## Executive Summary

The implementation covers roughly **60% of the designed surface area**. Core CMS primitives (Content Types, Entries, Media, Component Library/Editor, Pages structure) are built. The largest gaps are:

- **Sites Management** — no dedicated Sites page or per-site settings (0% built)
- **Visual Page Designer** — designed as a full-canvas drag-and-drop tool; only a form-based placeholder exists
- **AI Copilot** — fully designed three-panel chat interface; zero implementation
- **Settings Page** — only 4 fields implemented vs. 8 full sections in design
- **Entry Editor** — missing workflow steps, inline AI, SEO panel, and scheduling UI
- **Component Item Editor** — typed per-field UI collapsed into a single raw JSON field

---

## Feature Status Matrix

| Feature / Page | Design File | Route | Status | Priority |
|---|---|---|---|---|
| Dashboard | `index.html` | `/` | Partial | P2 |
| Sites List + New Site Wizard | `site-management.html` | `/sites` | **Missing** | P1 |
| Per-Site Settings | `site-settings.html` | `/sites/:id/settings` | **Missing** | P1 |
| Site Structure / IA Tree | `site-structure.html` | `/sites/:id/structure` | Partial | P1 |
| Content Types List | `content-types.html` | `/content-types` | Implemented | — |
| Content Type Schema Builder | `content-types.html` | `/content-types/:id/edit` | Implemented | — |
| Entry List (with folder tree) | `entry-list.html` | `/entries` | Partial | P2 |
| Entry Editor (workflow + AI + SEO) | `entry-editor.html` | `/entries/:id/edit` | Partial | P1 |
| Media Library (asset detail panel) | `media-library.html` | `/media` | Partial | P2 |
| Component Library | `component-library.html` | `/components` | Implemented | — |
| Component Schema Editor | `component-editor.html` | `/components/:id/edit` | Implemented | — |
| Component Item List | `component-item-list.html` | `/components/:id/items` | Implemented | — |
| Component Item Editor (typed fields) | `component-item-editor.html` | `/components/:id/items/:itemId` | Partial | P2 |
| Visual Page Designer | `page-designer.html` | `/pages/:id/design` | **Missing** | P2 |
| Settings (multi-section) | `settings.html` | `/settings` | Partial | P1 |
| AI Copilot | `ai-copilot.html` | `/ai-copilot` | **Missing** | P2 |
| Taxonomy | *(no design)* | `/taxonomy` | Extra | — |
| Tenants (SysAdmin) | *(no design)* | `/tenants` | Extra | — |
| Global Search Results | *(no design)* | `/search` | Extra | — |
| Install / Login | *(no design)* | `/install`, `/login` | Extra | — |

---

## Detailed Gap Specifications

### GAP-01 — Sites Management (P1 · Large)

**Design reference:** `site-management.html`, `site-settings.html`

**What design shows:**
- Stats strip: total sites / live / staging / total entries
- Grid of site cards with browser-mockup thumbnail, env badge (Live/Staging), URL, entry/content-type/page counts, locale chips (en-US, fr-FR, de-DE), per-card actions (Structure, Settings)
- "Add New Site" dashed card
- 4-step New Site Wizard modal:
  1. **Basics** — site name, auto-generated slug, description, timezone, template selector (Blank / Blog+News / E-commerce / Docs)
  2. **Domains** — domain rows with environment type (Production/Staging/Preview) and CORS origins
  3. **Locales** — locale picker with flag icons, fallback behaviour select
  4. **Done** — success step with next-action buttons
- Per-site Settings page (left nav + panels): General form, Domains table with SSL status badges, Locale drag-list with primary badge, API Keys cards (Delivery/Management/Preview) with copy/regenerate, Webhooks table with event chips and status, CDN/Cache toggle, Danger Zone (archive/delete)

**What is built:** `SiteContext.tsx` provides a dropdown site-switcher scoped inside AppShell. No `/sites` list route, no creation wizard, no per-site settings page.

**Work required:**

1. **API layer** (`src/api/sites.ts`)
   - `GET /api/sites` — list
   - `POST /api/sites` — create (name, slug, description, timezone, template, domains, locales)
   - `GET /api/sites/:id` — detail
   - `PUT /api/sites/:id` — update
   - `DELETE /api/sites/:id` — archive/delete
   - `GET/POST/DELETE /api/sites/:id/api-keys`
   - `GET/POST/PUT/DELETE /api/sites/:id/webhooks`
   - `GET/PUT /api/sites/:id/cdn`

2. **`SitesPage.tsx`** (`/sites`)
   - Stats strip component
   - `SiteCard` component (thumbnail placeholder via CSS canvas, env badge, locale chips)
   - `AddSiteCard` dashed card
   - `NewSiteWizard` modal — 4-step form with `useMultiStepForm` hook; validation per step via Zod

3. **`SiteSettingsPage.tsx`** (`/sites/:id/settings`)
   - Left nav with section anchors
   - `GeneralSection` form
   - `DomainsSection` — editable table with env type select + SSL badge
   - `LocalesSection` — drag-and-drop reorderable list (use `@dnd-kit/sortable`)
   - `ApiKeysSection` — cards with masked key, copy-to-clipboard, regenerate action
   - `WebhooksSection` — table with event chip multiselect, test/edit/delete
   - `CdnSection` — toggle + purge cache button
   - `DangerZoneSection` — confirmation-modal guarded archive + delete

4. **Sidebar** — add "Sites" nav item above "Content"

5. **Security considerations:**
   - API key display: mask by default, reveal on button press (prevent shoulder-surfing)
   - CORS origins field: validate URL format and disallow wildcard `*` on production domains
   - Webhook secrets: store as HMAC secrets, never return plain value after creation
   - Site deletion: require typed confirmation (`DELETE my-site-slug`) in modal

---

### GAP-02 — Site Structure / IA Tree (P1 · Medium)

**Design reference:** `site-structure.html`

**What design shows:**
- Site switcher tabs at top
- Left panel: collapsible tree with nested pages (toggle arrows, drag handles, inline "+ Add" hover actions, icon per page type)
- Center canvas: visual page-detail card
- Right detail panel: slug, page type, content type binding, layout selector, locale list

**What is built:** `PagesPage.tsx` at `/pages` — simple flat tree add/delete form + layout assignment. No drag reorder, no three-panel layout, no site tabs.

**Work required:**

1. Rename/re-route current `PagesPage.tsx` to become the Site Structure page at `/sites/:id/structure`
2. Implement three-panel shell (CSS Grid, collapsible panels)
3. Replace tree with `@dnd-kit` sortable tree (drag-to-reorder + drag-to-nest, indentation via `depth` metadata)
4. Right detail panel — controlled form bound to selected node: slug, pageType enum, contentTypeId select, layoutId select, locales multiselect
5. Add `PUT /api/pages/:id/reorder` endpoint to persist tree order + parentId changes atomically

---

### GAP-03 — AI Copilot (P2 · Large)

**Design reference:** `ai-copilot.html`

**What design shows:**
- Three-panel layout: left history list, center chat, right context panel
- History: conversation title, date, preview snippet
- Chat: user/AI message bubbles, inline citations, typing indicator, thumbs up/down feedback, mode pill (General / Content / SEO)
- Context panel: Current Entry card, Enabled Tools chips (Draft / Rewrite / SEO / Translate / Summarize / Extract), RAG Sources list
- Input: text field + "Attach Context" button + send button

**What is built:** `aiEnabled` boolean in SettingsPage. Nothing else.

**Work required:**

1. **API layer** (`src/api/aiCopilot.ts`)
   - `GET /api/ai/conversations` — list history
   - `POST /api/ai/conversations` — start new
   - `GET /api/ai/conversations/:id/messages` — message history
   - `POST /api/ai/conversations/:id/messages` — send (SSE streaming response)
   - `POST /api/ai/feedback` — record thumbs up/down

2. **`AiCopilotPage.tsx`** (`/ai-copilot`)
   - `ConversationList` sidebar
   - `ChatPane` — message list + streaming typing indicator via EventSource
   - `ChatMessage` — user/AI bubble, citations renderer, feedback buttons
   - `ContextPanel` — current-entry link, tool-chip toggles, RAG sources list
   - `ChatInput` — textarea with Shift+Enter multiline, attach context picker

3. **Inline AI in EntryEditor** (see GAP-05) — `AiSuggestionsPanel` component that calls `POST /api/ai/suggest` and renders Accept/Reject/Regenerate actions

4. **Dashboard AI widget** — AI usage card (budget bar, call counts by type) backed by `GET /api/ai/usage/summary`

5. **Sidebar** — add "AI Copilot" nav item in its own section

6. **Security considerations:**
   - All AI calls must include `tenantId` + `userId` in server-side audit log
   - Enforce per-tenant token budget server-side (not just UI cap) — reject with 429 when exceeded
   - Sanitise AI-generated HTML before inserting into Rich Text fields (XSS prevention)
   - Never send full entry content to AI without user explicitly attaching context

---

### GAP-04 — Settings Page — Multi-Section Expansion (P1 · Medium)

**Design reference:** `settings.html`

**What design shows (8 sections):**

| Section | Fields |
|---|---|
| General | Tenant name, slug, display domain, default locale, timezone, danger zone |
| Users & Roles | Team table, invite, per-user role select, MFA status |
| API Keys | Delivery / Management / Preview keys with copy, regenerate, revoke |
| Webhooks | URL, event multiselect, status badge, test/edit/delete |
| Plugins | Plugin cards with enable/disable toggle |
| AI Providers | Provider cards: OpenAI / Anthropic / Azure OpenAI; API key input, model select |
| Budgets & Limits | Monthly spend cap (USD), per-operation token limits |
| Safety & Governance | Content filter toggles, blocked-topic list |
| Auth & Security | Password policy (min length, complexity), MFA enforcement, IP allowlist, JWT expiry |

**What is built:** `SettingsPage.tsx` — 4 fields only: `displayName`, `timeZoneId`, `defaultLocale`, `aiEnabled`.

**Work required:**

1. Refactor `SettingsPage.tsx` to use a left-nav + scrollspy layout (matching `site-settings.html` pattern)
2. **ApiKeysSection** — same component as in Site Settings but scoped to tenant (Management key)
3. **WebhooksSection** — reuse same `WebhookTable` component (DRY)
4. **PluginsSection** — `PluginCard` with toggle, version, description
5. **AI sub-sections:**
   - `AiProvidersSection` — provider selection card group; each card: radio-select, API key input (masked), model select dropdown
   - `BudgetsSection` — monthly spend cap number input + per-operation sliders
   - `SafetySection` — toggle list for content filter categories
6. **AuthSecuritySection** — password policy form, MFA enforcement toggle, IP allowlist textarea (one CIDR per line, validate format), JWT expiry select
7. **API layer** (`src/api/settings.ts` extensions):
   - `GET/PUT /api/settings/ai-providers`
   - `GET/PUT /api/settings/security`
   - `GET/PUT /api/settings/plugins`

8. **Security considerations:**
   - AI provider API keys: never returned in GET responses; display masked `sk-...****` only
   - IP allowlist: validate CIDR on server; include current user's IP in warning ("You may lock yourself out")
   - MFA enforcement: adding enforcement should not lock out current session immediately — give 24-hour grace window

---

### GAP-05 — Entry Editor Enhancements (P1 · Medium)

**Design reference:** `entry-editor.html`

**Missing from current implementation:**

#### 5a. Workflow Steps Panel
- Visual stepper: Draft → Review → Approved → Published
- Current step highlighted; step-change action buttons ("Submit for Review", "Approve", "Request Changes")
- **API:** `POST /api/entries/:id/workflow/transition` with `{ action: 'submit' | 'approve' | 'reject' | 'publish' }`

#### 5b. Scheduled Publish
- Date-time picker ("Schedule" button in publish actions)
- Shows scheduled datetime badge on entry
- **API:** `POST /api/entries/:id/schedule` with `{ publishAt: ISO8601 }`

#### 5c. SEO Panel (right sidebar)
- Meta title (char counter, 60-char limit), meta description (160-char limit)
- Canonical URL input
- OG image picker
- Stored as `seoJson` field on entry
- **API:** extend entry PUT to include `seoData` object

#### 5d. Inline AI Suggestions Panel
- "AI Copilot" toggle button in entry editor header
- Collapsible right panel: prompt textarea + "Generate" button
- Response shows Accept / Reject / Regenerate actions
- Accepted text inserts into focused RTE field
- **API:** `POST /api/ai/suggest` with `{ fieldType, currentContent, prompt, entryContext }`

#### 5e. Entry List — Folder Tree Panel
- Left panel: folder/category tree with add-folder, collapse, counts
- Entries filtered by selected folder
- Schema / Webhooks / API Preview tabs in entry list header
- **API:** `GET/POST/PUT/DELETE /api/entries/folders`; `GET /api/entries?folderId=`

---

### GAP-06 — Media Library — Asset Detail Panel (P2 · Medium)

**Design reference:** `media-library.html`

**Missing from current implementation:**
- Clicking an asset opens a right sliding detail panel (not a separate page)
- Detail panel content:
  - Large preview (image) or file icon
  - Editable alt text field (auto-save on blur)
  - Focal point selector (click-on-image to set x/y percentages)
  - File metadata: dimensions (px), file size (formatted), MIME type, uploaded date, uploader name
  - Usage: "Used in N entries" with linked list
  - Copy URL button (with toast feedback)
  - Replace asset button (re-upload keeping same ID/URL)
  - Delete button (disabled if used in entries, with tooltip)

**Work required:**
1. Add `selectedAssetId` state to `MediaPage.tsx`
2. `AssetDetailPanel` component (sliding drawer, CSS transform animation)
3. **API:** `GET /api/media/:id/usage` → list of entry references
4. **API:** `PUT /api/media/:id` to update `altText`, `focalPoint`
5. **API:** `POST /api/media/:id/replace` for re-upload
6. Focal point: store as `{ x: number, y: number }` (0–1 percentage), render picker as overlay on image preview

---

### GAP-07 — Component Item Editor — Typed Field Inputs (P2 · Medium)

**Design reference:** `component-item-editor.html`

**What design shows:** Individual typed field editors rendered per-field based on the component schema:
- Short Text — `<input>` with character counter
- Long Text — `<textarea>` with character counter
- Rich Text — TipTap RTE toolbar + editor area
- Asset — thumbnail preview + "Select Asset" button (opens media picker modal)
- Number — number input with optional min/max validation display
- Boolean — toggle switch
- Date — date-time picker
- Entry Reference — entry picker modal (search + select)

**What is built:** `ComponentItemEditorPage.tsx` renders the entire `fieldsJson` as a single raw JSON `<textarea>`. No per-field type UI.

**Work required:**
1. `GET /api/components/:id` must return schema fields array (already done in component editor, confirm shape)
2. `DynamicFieldRenderer` component — shared with `EntryEditorPage.tsx`; accepts `{ field: FieldDef, value: unknown, onChange }` and returns the appropriate input for each `field.type`
3. Replace `fieldsJson` textarea in `ComponentItemEditorPage.tsx` with `DynamicFieldRenderer` array
4. `AssetPickerModal` component — shared between entry editor and component item editor
5. `EntryPickerModal` component — search + paginated list of entries filtered by content type

> Note: `EntryEditorPage.tsx` already does per-field rendering for entries; extract the rendering logic into the shared `DynamicFieldRenderer` component to avoid duplication.

---

### GAP-08 — Dashboard Enhancements (P2 · Small)

**Design reference:** `index.html`

**Missing from current dashboard:**
1. **Scheduled Publish Queue** — table of upcoming scheduled publishes (title, content type, datetime, Cancel button); backed by `GET /api/entries/scheduled`
2. **AI Usage Card** — budget progress bar, call count breakdown by type (Draft/Rewrite/SEO/Translate), monthly spend; backed by `GET /api/ai/usage/summary`
3. **Sites Mini-List** — 3-row list of sites with env badge and quick-link; backed by `GET /api/sites?limit=3`
4. **"Needs Your Review" Panel** — entries in `Review` workflow state assigned to current user; backed by `GET /api/entries?workflowState=review&assignee=me`
5. **"New Entry" CTA Button** — primary action button in dashboard header
6. **Stat card corrections** — design shows 4 cards (Total/Published/Pending Review/AI Actions Today); implementation shows 6 (different set). Align with design.

---

### GAP-09 — Visual Page Designer (P2 · Large)

**Design reference:** `page-designer.html`

**What design shows:**
- Full-viewport designer shell (no page scroll)
- Custom topbar: breadcrumb, undo/redo buttons, page name + URL, viewport toggle (desktop 1440 / tablet 768 / mobile 375), zoom percentage control, Preview button, Publish button
- Left panel: draggable component palette organised by zone category
- Center canvas: scaled `<iframe>` preview with zone drop areas highlighted; selected component has a blue selection border
- Right properties panel: selected component's field inputs + slot configuration
- Toggle between Design mode and Preview mode (full preview with no overlays)

**Work required:**
1. `PageDesignerPage.tsx` (`/pages/:id/design`) — new full-viewport shell (overflow: hidden on body)
2. `DesignerTopbar` — breadcrumb, undo/redo (`useUndoRedo` hook backed by immer patches), viewport selector buttons, zoom select, publish button
3. `ComponentPalette` — left panel, grouped component tiles, `draggable` from `@dnd-kit/core`
4. `DesignerCanvas` — `<iframe>` preview rendered at `width / scale` then CSS-scaled; `DragOverlay` highlights zone drop areas
5. `PropertiesPanel` — right panel, renders selected component's fields via `DynamicFieldRenderer`
6. `usePageDesigner` hook — manages: selected component id, drag state, undo history, viewport, zoom
7. **API:** `GET /api/pages/:id/design` → full design tree (zones → placed components → field values)
8. **API:** `PUT /api/pages/:id/design` → save design tree (debounced auto-save + explicit Publish)
9. **Security:** design saves are draft-only until Publish is clicked; iframe preview must use a sandboxed renderer endpoint (not live public URL) to prevent CSRF via designer

---

## Implementation Sequencing

### Phase 1 — Foundation & High-Value Gaps (Weeks 1–3)

| Task | Gap Ref | Effort |
|---|---|---|
| Sites Management — API + SitesPage + NewSiteWizard | GAP-01 | L |
| Per-Site Settings page | GAP-01 | M |
| Settings Page — multi-section expansion | GAP-04 | M |
| Entry Editor — Workflow Steps + Scheduling | GAP-05a, 05b | M |
| Site Structure — three-panel + dnd-kit tree | GAP-02 | M |

### Phase 2 — Editor Completeness (Weeks 4–5)

| Task | Gap Ref | Effort |
|---|---|---|
| DynamicFieldRenderer shared component | GAP-07 | M |
| Component Item Editor — typed fields | GAP-07 | S |
| Entry Editor — SEO panel | GAP-05c | S |
| Entry List — folder tree panel | GAP-05e | M |
| Media Library — asset detail panel | GAP-06 | M |
| Dashboard enhancements | GAP-08 | S |

### Phase 3 — AI Features (Weeks 6–8)

| Task | Gap Ref | Effort |
|---|---|---|
| AI Copilot page | GAP-03 | L |
| Entry Editor — inline AI suggestions panel | GAP-05d | M |
| Dashboard AI usage card | GAP-08 | S |
| Settings — AI providers + budgets + safety | GAP-04 | M |

### Phase 4 — Visual Page Designer (Weeks 9–11)

| Task | Gap Ref | Effort |
|---|---|---|
| PageDesignerPage shell + topbar | GAP-09 | L |
| DnD canvas + zone highlighting | GAP-09 | L |
| Properties panel + undo/redo | GAP-09 | M |
| Designer preview iframe renderer | GAP-09 | M |

---

## New Routes to Add

```
/sites                              → SitesPage
/sites/:id/settings                 → SiteSettingsPage
/sites/:id/structure                → SiteStructurePage  (replaces /pages)
/pages/:id/design                   → PageDesignerPage
/ai-copilot                         → AiCopilotPage
/ai-copilot/:conversationId         → AiCopilotPage (with conversation pre-loaded)
```

---

## New Shared Components to Build

| Component | Used By |
|---|---|
| `DynamicFieldRenderer` | EntryEditor, ComponentItemEditor, PageDesigner properties panel |
| `AssetPickerModal` | EntryEditor, ComponentItemEditor |
| `EntryPickerModal` | EntryEditor (Entry Reference fields), ComponentItemEditor |
| `WebhookTable` | SiteSettings, TenantSettings |
| `ApiKeyCard` | SiteSettings, TenantSettings |
| `LeftNavSettings` | SiteSettings, TenantSettings (shared nav+scrollspy shell) |
| `NewSiteWizard` | SitesPage |
| `AiSuggestionsPanel` | EntryEditor |
| `WorkflowStepper` | EntryEditor |
| `SeoPanel` | EntryEditor |
| `AssetDetailPanel` | MediaPage |
| `DraggablePageTree` | SiteStructurePage |

---

## Security Checklist (per Project Instructions)

- [ ] All new API routes require `[Authorize]` attribute with appropriate role policy
- [ ] AI provider API keys: store encrypted at rest (AES-256); never returned in API responses
- [ ] Webhook secrets: store as HMAC secret; send `X-MicroCMS-Signature` header on delivery
- [ ] Site deletion: server-side check that no active entries exist before hard delete
- [ ] IP allowlist: server-side enforcement in middleware, not just UI validation
- [ ] AI output sanitisation: strip script tags and event handlers before inserting into RTE
- [ ] AI token budget: enforce server-side with Redis counter, 429 with `Retry-After` when exceeded
- [ ] Page designer iframe: use sandboxed preview renderer route, not public site URL
- [ ] CORS origins: validate URL format + disallow wildcard `*` on production-type domains
- [ ] Scheduled publish: use background job queue (Hangfire/hosted service), not client-side timer
- [ ] All new server endpoints: validate inputs via FluentValidation; return 400 with field-level errors
- [ ] Avoid storing sensitive form values in React state longer than needed (clear API key inputs on save)

---

## Design Files with No Current Implementation Gap (Already Done)

- `content-types.html` → `ContentTypesPage.tsx` + `ContentTypeEditPage.tsx` ✅
- `component-library.html` → `ComponentLibraryPage.tsx` ✅
- `component-editor.html` → `ComponentEditorPage.tsx` ✅ (very close match)
- `component-item-list.html` → `ComponentItemListPage.tsx` ✅

---

*This document should be updated as features move from "Missing" to "Implemented" status.*
