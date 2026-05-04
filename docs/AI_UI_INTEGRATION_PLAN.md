# AI API Integration Points — Admin UI

**Version:** 1.0  
**Last Updated:** 2026-04-29  
**Status:** Planning — ready for implementation sprint

---

## Overview

This document catalogues every place in the Admin UI that either already has an AI affordance (stub button/panel) or logically should have one, maps each to the backend API endpoint that serves it, and records the current wiring status.

### Backend AI Endpoints (available today)

| Method | Route | Purpose | Backend class |
|--------|-------|---------|----------------|
| `POST` | `/api/v1/ai/drafts/generate` | Generate a full entry draft from a prompt | `AiController.GenerateDraft` |
| `POST` | `/api/v1/aiwriting/{entryId}/draft` | Draft content into a specific entry field | `AiWritingController.Draft` |
| `POST` | `/api/v1/aiwriting/{entryId}/rewrite` | Rewrite a field with custom instructions | `AiWritingController.Rewrite` |
| `POST` | `/api/v1/aiwriting/{entryId}/tone` | Change the tone of a field | `AiWritingController.ChangeTone` |
| `GET`  | `/api/v1/aiwriting/{entryId}/summarize` | Summarise a field | `AiWritingController.Summarize` |
| `POST` | `/api/v1/aiwriting/{entryId}/translate` | Translate entry to a new locale | `AiWritingController.Translate` |
| `POST` | `/api/v1/media/{assetId}/alt-text` | Generate AI alt text for an image asset | `MediaController` *(endpoint missing — handler exists)* |
| `GET`  | `/api/v1/entries/{entryId}/quality-checks` | Grammar, readability, PII scan | `EntriesController` *(endpoint missing — handler exists)* |

> **Note — two missing endpoints:** `GenerateAltTextCommandHandler` and `RunQualityChecksQueryHandler` both exist in the Application layer but have no controller action yet. These must be added before the UI integrations for those features can be completed.

---

## Integration Point Catalogue

### 1. Entry Editor — AI Authoring Assist Panel

**File:** `src/pages/entries/EntryEditorPage.tsx` — `AiAuthoringPanel` component (right sidebar)  
**Current state:** Five static buttons rendered with hardcoded labels; **no API calls wired**.

| Button | API Call | Endpoint | Request shape |
|--------|----------|----------|---------------|
| **Generate Draft** | `POST` | `/api/v1/ai/drafts/generate` | `{ siteId, contentTypeId, prompt }` |
| **Rewrite / Tone** | `POST` | `/api/v1/aiwriting/{entryId}/rewrite` or `/tone` | `{ fieldHandle, instructions }` / `{ fieldHandle, tone }` |
| **Summarize** | `GET` | `/api/v1/aiwriting/{entryId}/summarize?fieldHandle=&maxSentences=` | query params |
| **SEO Suggestions** | *(see §5 — Page SEO)* | No entry-level SEO endpoint yet | — |
| **Translate** | `POST` | `/api/v1/aiwriting/{entryId}/translate` | `{ sourceLocale, targetLocale }` |

**Implementation tasks:**
- Create `src/api/ai.ts` with typed wrappers for all AI endpoints.
- Add `AiContentResult` and `GeneratedDraftDto` types to `src/types/index.ts`.
- Convert `AiAuthoringPanel` from a static list to an interactive drawer/modal:
  - **Generate Draft**: show prompt textarea → call `/ai/drafts/generate` → populate form fields.
  - **Rewrite / Tone**: show field selector + instruction/tone picker → call `/aiwriting/{id}/rewrite` or `/tone` → replace field value with confirmation.
  - **Summarize**: show field selector → call `/aiwriting/{id}/summarize` → display result in a read-only panel with a "Use this" button.
  - **Translate**: show source/target locale dropdowns → call `/aiwriting/{id}/translate` → navigate to the new locale tab.
- Disable all AI buttons when `aiEnabled === false` on the current site.
- Show a 429 toast when the budget is exhausted.

---

### 2. Entry Editor — Inline "Generate with AI" on Text Fields

**File:** `src/pages/entries/EntryEditorPage.tsx` — `ScalarFieldInput` (ShortText/LongText) and RichText `"AI Assist"` button  
**Current state:** Buttons are rendered but are `type="button"` with no `onClick` handler — dead UI.

| Field type | Trigger | API Call | Notes |
|-----------|---------|----------|-------|
| `ShortText` | "✦ Generate with AI" badge-button next to char counter | `POST /api/v1/aiwriting/{entryId}/draft` (field-scoped) | Pre-fill `fieldHandle`; replace value on accept |
| `LongText` / `Markdown` | Same badge-button | `POST /api/v1/aiwriting/{entryId}/draft` (field-scoped) | |
| `RichText` | "✦ AI Assist" floating button top-right of editor | Opens AI assist drawer (Rewrite / Tone / Summarize) | `entryId` + `fieldHandle` pre-wired |

**Implementation tasks:**
- Wire the `onClick` for each button to open a small popover/modal pre-scoped to that `field.handle`.
- After API call returns, show a diff preview ("Accept / Discard").
- Disable buttons on new (unsaved) entries — `entryId` is required for `/aiwriting` endpoints.

---

### 3. Entry Editor — Quality Checks Panel

**File:** `src/pages/entries/EntryEditorPage.tsx` — `QualityChecksPanel` component  
**Current state:** Panel renders **hardcoded mock data** — grammar ✓, readability Grade 9, no PII, missing locale.

| Action | API Call | Endpoint |
|--------|----------|----------|
| Auto-run on load (existing entries) | `GET` | `/api/v1/entries/{entryId}/quality-checks` *(endpoint to be added)* |
| "Re-run" button | `GET` | Same |

**Implementation tasks:**
- Add `GET /api/v1/entries/{entryId}/quality-checks` controller action wired to `RunQualityChecksQuery`.
- Add `qualityChecks` method to `src/api/entries.ts`.
- Add `QualityCheckReport` type to `src/types/index.ts`.
- Replace hardcoded array in `QualityChecksPanel` with a `useQuery` call.
- Show spinner while loading; show "Re-run" button in header.
- Map `grammarScore` → pass/warn/fail thresholds (≥0.85 pass, ≥0.65 warn, else fail).
- Map `readabilityGrade` and display `suggestions` as an expandable list.
- Highlight PII matches in a red banner with redacted values shown.

---

### 4. Media Library — AI Alt Text Generation

**File:** `src/pages/media/MediaPage.tsx` — `AssetDetail` panel  
**Current state:** Alt Text is a plain `<input>` with manual editing only. No AI generation button exists.

| Action | API Call | Endpoint |
|--------|----------|----------|
| "✦ Generate" button next to Alt Text input | `POST` | `/api/v1/media/{assetId}/alt-text` *(endpoint to be added)* |

**Implementation tasks:**
- Add `POST /api/v1/media/{assetId}/alt-text` controller action wired to `GenerateAltTextCommand`.
- Add `generateAltText(assetId: string)` method to `src/api/media.ts`.
- Render a "✦ Generate" icon-button alongside the Alt Text `<input>` in `AssetDetail`.
- Only show for `mediaType === 'image'` and `status === 'Available'`.
- On success, populate the alt text input and auto-dirty the form (so Save becomes active).
- Show spinner during generation; show 429 toast if budget exhausted.

---

### 5. Pages — AI SEO Suggestions

**File:** `src/pages/pages/PageDetailPanel.tsx` — `SeoEditor` component  
**Current state:** SEO fields (meta title, meta description, canonical, og:image) are all manual inputs. No AI assist button.

| Action | API Call | Endpoint | Notes |
|--------|----------|----------|-------|
| "✦ Suggest with AI" button | `POST` | `/api/v1/pages/{pageId}/seo/suggest` *(new endpoint needed)* | Requires new Application command + controller action |

**Backend work required:**
- Create `SuggestPageSeoCommand(Guid PageId)` → `PageSeoSuggestionDto(MetaTitle, MetaDescription, Keywords[])` in Application layer.
- Wire to `ILlmService` using `AI.Page.SEOSystemPrompt` / `AI.Page.SEOUserPrompt` from settings.
- Add `POST /api/v1/pages/{pageId}/seo/suggest` action to `PagesController`.

**UI implementation tasks:**
- Add `suggestSeo(pageId: string)` to `src/api/pages.ts`.
- Add a "✦ Suggest with AI" button in the `SeoEditor` header.
- On success, populate all three text fields (title, description, keywords) with a yellow "AI draft — accept?" banner.
- Accept/Discard pattern to avoid overwriting manually-entered values.

---

### 6. Entries List — "Generate Entry" Quick-Create

**File:** `src/pages/entries/EntriesPage.tsx`  
**Current state:** "New Entry" button navigates to blank editor. No AI-assisted quick-create flow.

| Action | API Call | Endpoint |
|--------|----------|----------|
| "✦ Generate…" button in page header | `POST` | `/api/v1/ai/drafts/generate` |

**Implementation tasks:**
- Add a "✦ Generate…" secondary button alongside "New Entry" (only visible when `aiEnabled`).
- Opens a modal: content type selector + prompt textarea + locale.
- On success: navigate to `EntryEditorPage` with form pre-populated from `GeneratedDraftDto.fields`.
- Pass `?contentTypeId=` in the URL as is already supported by `EntryEditorPage`.

---

### 7. Settings — AI Settings Tab (Tenant-level)

**File:** `src/pages/settings/SettingsPage.tsx`  
**Current state:** Only a single `aiEnabled` checkbox. No provider/endpoint/budget CRUD.

**Implementation tasks:**
- Add an `"AI"` tab to the tab bar in `SettingsPage` (alongside `"general"`, `"security"`, etc.).
- Reuse the `AiSettingField` component already implemented in `SiteSettingPage.tsx` — extract it into a shared component at `src/components/ai/AiSettingField.tsx`.
- The tenant-level tab should call `sitesApi.upsertConfigEntry` on the **tenant's primary site** (or a dedicated tenant-config endpoint if one is added).
- Render the same `AI_SECTIONS` structure as `SiteSettingPage` (Provider, Generation Defaults, Budget, Safety, Prompts).
- Note: `SiteSettingPage` already has the full AI settings tab — this item is about **mirroring it at the tenant level**.

---

### 8. Site Settings — AI Settings Tab

**File:** `src/pages/sites/SiteSettingPage.tsx`  
**Current state:** ✅ **Fully implemented.** The AI settings tab renders all `AI_SECTIONS` (Provider, Generation Defaults, Budget, Safety, Entry/Page prompts) using `AiSettingField` components that call `sitesApi.upsertConfigEntry` / `deleteConfigEntry`.

No further work needed here.

---

### 9. Dashboard — AI Usage / Budget Widget

**File:** `src/pages/dashboard/DashboardPage.tsx`  
**Current state:** No AI usage information shown on the dashboard.

| Widget | API Call | Endpoint | Notes |
|--------|----------|----------|-------|
| Token usage today vs. daily cap | `GET` | `/api/v1/ai/usage` *(new endpoint needed)* | Requires `BudgetService` to expose usage stats |
| Provider status badge | Same | Same | Shows configured provider name + "Active / Not configured" |

**Backend work required:**
- Add `GetAiUsageQuery(Guid SiteId)` → `AiUsageDto(Provider, TokensUsedToday, DailyCapTokens, MonthlyCostUsd, MonthlyCostCapUsd)`.
- Expose `GET /api/v1/ai/usage?siteId=` in `AiController`.

**UI implementation tasks:**
- Add `getUsage(siteId: string)` to `src/api/ai.ts`.
- Render a compact dashboard card: token progress bar + provider badge.
- Only render when `aiEnabled === true` on the active site.

---

## New File: `src/api/ai.ts`

This file does not currently exist. It must be created to centralise all AI API calls.

```typescript
// src/api/ai.ts — typed wrappers for all AI endpoints

export interface AiContentResult {
  generatedText: string;
  promptTokens: number;
  completionTokens: number;
  providerName: string;
}

export interface GeneratedDraftDto {
  contentTypeId: string;
  fields: Record<string, unknown>;
  promptTokens: number;
  completionTokens: number;
  providerName: string;
}

export interface QualityCheckReport {
  entryId: string;
  grammarScore: number;
  readabilityGrade: string;
  piiDetected: boolean;
  piiMatches: { type: string; redactedValue: string; position: number }[];
  suggestions: string[];
}

export interface PageSeoSuggestionDto {
  metaTitle: string;
  metaDescription: string;
  keywords: string[];
}

export interface AiUsageDto {
  provider: string;
  tokensUsedToday: number;
  dailyCapTokens: number;
  monthlyCostUsd: number;
  monthlyCostCapUsd: number;
}
```

---

## New Shared Component: `src/components/ai/AiAssistDrawer.tsx`

A reusable slide-in drawer used by both `AiAuthoringPanel` (entry sidebar) and inline field buttons.

Props:
- `entryId: string`
- `contentTypeId: string`
- `fields: FieldDefinitionDto[]`
- `onApply: (fieldHandle: string, value: string) => void`
- `onClose: () => void`
- `defaultAction?: 'draft' | 'rewrite' | 'tone' | 'summarize' | 'translate'`
- `defaultFieldHandle?: string`

Actions rendered inside the drawer:
1. **Generate Draft** — prompt textarea + call `/ai/drafts/generate`
2. **Rewrite** — field selector + instruction textarea + call `/aiwriting/{id}/rewrite`
3. **Change Tone** — field selector + tone dropdown (`Professional | Casual | Friendly | Formal | Persuasive | Empathetic`) + call `/aiwriting/{id}/tone`
4. **Summarize** — field selector + max-sentences slider + call `/aiwriting/{id}/summarize`
5. **Translate** — source locale + target locale dropdowns + call `/aiwriting/{id}/translate`

All actions show a "Generated result" preview with **Accept** / **Discard** buttons before writing to the form.

---

## Summary: Implementation Priority

| # | Integration Point | Effort | Blocked by |
|---|-------------------|--------|------------|
| 1 | Create `src/api/ai.ts` + types | XS | — |
| 2 | Entry Editor: wire `AiAuthoringPanel` → drawer | M | `ai.ts` |
| 3 | Entry Editor: wire inline field "Generate with AI" buttons | S | `ai.ts` |
| 4 | Media: AI alt-text button | S | Add missing controller endpoint |
| 5 | Entry Editor: live Quality Checks panel | M | Add missing controller endpoint |
| 6 | Entries List: "Generate…" quick-create modal | S | `ai.ts` |
| 7 | Pages: AI SEO suggestions | M | New Application command + endpoint |
| 8 | Dashboard: AI usage widget | S | New Application query + endpoint |
| 9 | Settings (tenant-level): AI tab | S | Extract shared `AiSettingField` |

**Total estimated items:** 9  
**Backend-only blockers:** §4 (alt-text endpoint), §5 (quality-checks endpoint), §7 (SEO suggestion command+endpoint), §8 (usage query+endpoint)  
**Pure UI work (no backend changes):** §1, §2, §3, §6, §9
