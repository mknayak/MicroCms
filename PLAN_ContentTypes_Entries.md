# ContentTypes & Entries — Gap Analysis & Implementation Plan

**Prepared:** 2026-04-26  
**Scope:** ContentTypes and Entries features — Design HTML → Frontend → Backend → Database  
**Based on:** Full read of design HTMLs, all React pages/API files, all C# controllers/entities, EF migrations

---

## Executive Summary

The ContentTypes and Entries features have a working skeleton but carry significant gaps across every layer. The most critical issues are:

1. **Critical contract mismatches** between frontend and backend that cause silent data corruption or runtime 404s (fields sent as object vs. expected as JSON string; wrong route names; missing DTO fields)
2. **DB migration gap** — `FolderId`, `SeoMetaTitle/Description/CanonicalUrl/OgImage` columns are configured in EF but absent from the only migration
3. **Missing `IsIndexed` column** on `ContentTypeFields`
4. **ContentType card grid** is implemented as a flat table; the New CT modal is missing site selector, template, and localization mode
5. **Entry Editor** lacks the Workflow stepper, SEO panel, field-specific pickers (media, entry-ref, taxonomy), schedule UI, and Preview link
6. **Entry List** lacks folder tree, locale/author filters, bulk actions, and all three secondary tabs

---

## Part 1 — Database

### DB-01 · Missing columns in `Entries` table (CRITICAL)

**Problem:** `EntryConfiguration.cs` maps `FolderId`, `SeoMetaTitle`, `SeoMetaDescription`, `SeoCanonicalUrl`, `SeoOgImage` — but the sole migration `20250101000000_InitialCreate` never emits these columns. The DB schema diverges from the EF model; any query that touches SEO or FolderId will throw at runtime.

**Fix — add migration `20260426_AddEntrySeoPlusFolderAndIndexedField`:**

```csharp
// Entries table additions
migrationBuilder.AddColumn<string>(
    name: "FolderId", table: "Entries",
    type: "TEXT", maxLength: 36, nullable: true);

migrationBuilder.AddColumn<string>(
    name: "SeoMetaTitle", table: "Entries",
    type: "TEXT", maxLength: 60, nullable: true);

migrationBuilder.AddColumn<string>(
    name: "SeoMetaDescription", table: "Entries",
    type: "TEXT", maxLength: 160, nullable: true);

migrationBuilder.AddColumn<string>(
    name: "SeoCanonicalUrl", table: "Entries",
    type: "TEXT", maxLength: 500, nullable: true);

migrationBuilder.AddColumn<string>(
    name: "SeoOgImage", table: "Entries",
    type: "TEXT", maxLength: 500, nullable: true);

migrationBuilder.CreateIndex(
    name: "IX_Entries_FolderId",
    table: "Entries", column: "FolderId");
```

### DB-02 · Missing `IsIndexed` column on `ContentTypeFields`

**Problem:** The design Schema Tab shows an "Indexed" toggle per field. The EF entity `FieldDefinition` has no `IsIndexed` property and `ContentTypeFields` has no column for it. Setting a field as indexed is part of the design's schema management.

**Fix — add to same migration:**

```csharp
migrationBuilder.AddColumn<bool>(
    name: "IsIndexed", table: "ContentTypeFields",
    type: "INTEGER", nullable: false, defaultValue: false);
```

**Also add to EF entity and configuration:**

```csharp
// FieldDefinition.cs
public bool IsIndexed { get; private set; }

// FieldDefinitionConfiguration.cs (or ContentTypeConfiguration)
builder.Property(f => f.IsIndexed).IsRequired();
```

### DB-03 · Missing `EntryFolders` table

**Problem:** The design shows a hierarchical folder tree (Custom Folders with nesting, counts). `FolderId` alone is insufficient without a table to define the folders.

**Fix — add table:**

```csharp
migrationBuilder.CreateTable(
    name: "EntryFolders",
    columns: table => new {
        Id        = table.Column<string>(type:"TEXT", maxLength:36, nullable:false),
        TenantId  = table.Column<string>(type:"TEXT", maxLength:36, nullable:false),
        SiteId    = table.Column<string>(type:"TEXT", maxLength:36, nullable:false),
        ParentId  = table.Column<string>(type:"TEXT", maxLength:36, nullable:true),
        Name      = table.Column<string>(type:"TEXT", maxLength:200, nullable:false),
        SortOrder = table.Column<int>(nullable:false, defaultValue:0),
        CreatedAt = table.Column<DateTimeOffset>(nullable:false),
        UpdatedAt = table.Column<DateTimeOffset>(nullable:false)
    },
    constraints: table => {
        table.PrimaryKey("PK_EntryFolders", x => x.Id);
        table.ForeignKey("FK_EntryFolders_Parent",
            x => x.ParentId, "EntryFolders", "Id",
            onDelete: ReferentialAction.SetNull);
    });

migrationBuilder.CreateIndex("IX_EntryFolders_SiteId",
    "EntryFolders", "SiteId");
```

### DB-04 · Missing `LocalizationMode` column on `ContentTypes`

**Problem:** The design Create modal shows a "Localization" selector (Per-locale fields / Shared). No column exists on the `ContentTypes` table.

**Fix:**

```csharp
migrationBuilder.AddColumn<string>(
    name: "LocalizationMode", table: "ContentTypes",
    type: "TEXT", maxLength: 32, nullable: false, defaultValue: "PerLocale");
// Values: "PerLocale" | "Shared"
```

**Add to domain entity and EF config accordingly.**

---

## Part 2 — Backend (C# / ASP.NET Core)

### BE-01 · Fix `EntryListItemDto` — missing display fields (CRITICAL)

**Problem:** `EntryListItemDto` returns only `{ Id, SiteId, ContentTypeId, Slug, Locale, Status, CurrentVersionNumber, CreatedAt, UpdatedAt, PublishedAt }`. The frontend `EntryListItem` type expects `title`, `authorName`, and `contentTypeName` — none of which are projected. The list table cannot render correctly.

**Fix — extend `EntryListItemDto`:**

```csharp
public sealed record EntryListItemDto(
    Guid Id,
    Guid SiteId,
    Guid ContentTypeId,
    string ContentTypeName,   // ADD
    string Slug,
    string? Title,            // ADD — extracted from FieldsJson["title"] if present
    string Locale,
    string AuthorId,
    string AuthorName,        // ADD — join to Users
    EntryStatus Status,
    int CurrentVersionNumber,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? PublishedAt
);
```

**Update `ListEntriesQueryHandler`:**
- Join to `ContentTypes` to project `DisplayName` as `ContentTypeName`
- Join to `Users` (or `ApplicationUsers`) to project `DisplayName` as `AuthorName`
- Deserialize `FieldsJson` partially to extract `title` key: use `System.Text.Json.JsonDocument` to peek at the title field without full deserialization

**Also add `localeVariants` to `EntryDto`:**

```csharp
// GetEntryQueryHandler: query all locales for same (SiteId, ContentTypeId, Slug)
public IReadOnlyList<string> LocaleVariants { get; init; }
```

### BE-02 · Fix route `/entries/{id}/submit` vs frontend's `/entries/{id}/review` (CRITICAL)

**Problem:** Frontend calls `POST /entries/{id}/review` but the controller route is `POST /entries/{id}/submit`.

**Decision:** Fix the backend route to match the design's vocabulary (`submit`) AND update the frontend. The backend name `submit` is semantically cleaner.

```csharp
// EntriesController.cs — already correct:
[HttpPost("{id}/submit")]
public async Task<IActionResult> SubmitForReview(Guid id) { ... }
```

**Frontend fix (see FE-05):** Change `entriesApi.submitForReview()` to call `/entries/{id}/submit`.

### BE-03 · Fix version restore contract mismatch (CRITICAL)

**Problem:** Frontend calls `POST /entries/{id}/versions/{versionId}/restore` (by version GUID). Backend route is `POST /entries/{id}/rollback` with body `{ TargetVersionNumber: int }`.

**Fix — add a REST-aligned route that accepts version GUID:**

```csharp
[HttpPost("{id}/versions/{versionId}/restore")]
public async Task<IActionResult> RestoreVersion(Guid id, Guid versionId,
    CancellationToken ct)
{
    // Look up the version number from the GUID, then delegate to rollback handler
    var versionNumber = await _mediator.Send(
        new GetVersionNumberByIdQuery(id, versionId), ct);
    var result = await _mediator.Send(
        new RollbackEntryCommand(id, versionNumber, CurrentUserId), ct);
    return result.IsSuccess ? Ok(result.Value) : Problem(result.Error);
}
```

Alternatively keep `rollback` and fix the frontend (FE-06) — but adding the REST path is more forward-compatible.

### BE-04 · Fix `FieldsJson` serialization boundary (CRITICAL)

**Problem:** Backend stores and returns `FieldsJson` as a raw `string`. Frontend types `Entry.fields` as `Record<string, unknown>` (a parsed object). Neither side currently converts. If the API client sends an object where a string is expected, the field data will be double-serialized or lost.

**Fix — expose fields as a parsed object in the DTO, not as a raw string:**

```csharp
// EntryDto.cs — replace FieldsJson string with parsed dictionary
public Dictionary<string, JsonElement> Fields { get; init; }
// Serialized by System.Text.Json automatically as a nested JSON object

// EntryVersionDto.cs — same change
public Dictionary<string, JsonElement> Fields { get; init; }
```

Update `GetEntryQueryHandler` to deserialize once:
```csharp
Fields = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(entry.FieldsJson)
         ?? new Dictionary<string, JsonElement>()
```

Update `UpdateEntryCommand` to accept `Dictionary<string, object>` and serialize to `FieldsJson` inside the handler:
```csharp
public sealed record UpdateEntryCommand(
    Guid EntryId,
    Guid UserId,
    string? NewSlug,
    Dictionary<string, object> Fields,   // was FieldsJson string
    string? ChangeNote
);
// Handler: entry.UpdateFields(JsonSerializer.Serialize(command.Fields), ...)
```

Update `CreateEntryCommand` the same way.

### BE-05 · Align `EntryStatus` enum naming with design vocabulary

**Problem:** Backend uses `PendingApproval` but design shows "Pending Review". Frontend uses `'Review'`. This causes badge/filter mismatches.

**Fix — rename backend enum value** (or add a display-name attribute):

```csharp
public enum EntryStatus
{
    Draft = 0,
    PendingReview = 1,   // was PendingApproval — rename across codebase
    Approved = 2,
    Published = 3,
    Unpublished = 4,
    Archived = 5,
    Scheduled = 6
}
```

Update all switch statements, FluentValidation rules, and EF string conversion configuration.

### BE-06 · Fix `ListEntriesQuery` pagination parameter name

**Problem:** Frontend sends query param `pageNumber` but `ListEntriesQuery` uses property name `page`.

**Fix — rename in query/handler to match frontend:**

```csharp
public sealed record ListEntriesQuery(
    Guid SiteId,
    EntryStatus? Status = null,
    Guid? ContentTypeId = null,
    string? Locale = null,
    string? Search = null,
    Guid? FolderId = null,      // ADD for folder tree filtering
    int PageNumber = 1,         // was Page
    int PageSize = 20
);
```

Update controller binding: `[FromQuery] int pageNumber = 1`.

### BE-07 · `ContentTypesController` — add missing endpoints

**Problem:** The design requires Import Schema, entry-count in list, and locale-count. The backend also validates handle as `[A-Za-z0-9_]+` only — design shows hyphenated API slugs.

**Fixes:**

**a) Add `EntryCount` and `LocaleCount` to `ContentTypeListItemDto`:**

```csharp
public sealed record ContentTypeListItemDto(
    Guid Id,
    string Handle,
    string DisplayName,
    string Status,
    int FieldCount,
    int EntryCount,       // ADD — count of non-archived entries
    int LocaleCount,      // ADD — count of distinct locales in entries
    DateTimeOffset UpdatedAt
);
```

**b) Add `LocalizationMode` to create/update commands and DTOs.**

**c) Add import endpoint:**

```csharp
[HttpPost("import")]
[Authorize(Policy = "ContentAdmin")]
public async Task<IActionResult> ImportSchema(
    [FromBody] ImportContentTypeSchemaRequest request, CancellationToken ct)
```

`ImportContentTypeSchemaRequest`: accepts a JSON Schema object or array; parses field definitions and creates a new ContentType in Draft status.

**d) Allow hyphens in `Handle`** — change validator regex from `^[A-Za-z0-9_]+$` to `^[a-z0-9][a-z0-9-]*[a-z0-9]$` to align with design's `api/v1/blog-post` pattern.

### BE-08 · Add `IsIndexed` to `FieldDefinition` domain and commands

```csharp
// Domain
public bool IsIndexed { get; private set; }

// AddFieldCommand / UpdateFieldCommand — add IsIndexed parameter
// Handler: pass through to FieldDefinition constructor/update method
```

### BE-09 · `EntryFolders` CRUD controller

New `EntryFoldersController`:

```
GET    /api/v1/sites/{siteId}/folders          → list folders as tree
POST   /api/v1/sites/{siteId}/folders          → create folder
PUT    /api/v1/sites/{siteId}/folders/{id}     → rename / move (change parentId)
DELETE /api/v1/sites/{siteId}/folders/{id}     → delete (entries moved to parent)
```

### BE-10 · Security hardening

- `ContentTypesController.Delete`: add domain check — reject if `EntryCount > 0` with a `409 Conflict` and message "Delete all entries before removing a content type."
- `EntriesController.BulkDelete`: require explicit confirmation token in body (not just an array of IDs) to prevent accidental mass delete.
- `EntriesController.Schedule`: validate `PublishAt > UtcNow + 1 minute`; reject past datetimes.
- `ImportSchema`: validate imported JSON against a strict schema shape before processing; cap at 50 fields per import to prevent DoS.
- All entry-mutating endpoints: validate `AuthorId == CurrentUserId` OR caller has `ContentAdmin` role.

---

## Part 3 — Frontend (React / TypeScript)

### FE-01 · Fix `fields` serialization in `api/entries.ts` (CRITICAL)

**Problem:** After BE-04 fix, the backend will now accept and return `fields` as a JSON object (not a string). The API layer must be updated to match.

**Fix in `api/entries.ts`:**

```typescript
// Remove any JSON.stringify/parse workarounds — fields is now a plain object
export async function createEntry(req: CreateEntryRequest): Promise<Entry> {
  // req.fields is Record<string, unknown> — sent directly as JSON object
  const res = await client.post('/entries', req);
  return res.data; // fields returned as parsed object directly
}
```

Remove any manual `JSON.parse(entry.fieldsJson)` or `JSON.stringify(entry.fields)` that may have been added as workarounds.

### FE-02 · Align `EntryStatus` type (CRITICAL)

**Fix in `types/index.ts`:**

```typescript
export type EntryStatus =
  | 'Draft'
  | 'PendingReview'    // was 'Review' — align with backend rename
  | 'Approved'         // ADD
  | 'Published'
  | 'Unpublished'      // ADD
  | 'Archived'
  | 'Scheduled';
```

Update all badge renderers, filter dropdowns, and switch statements in `EntriesPage.tsx` and `EntryEditorPage.tsx`.

### FE-03 · Fix `submitForReview` route (CRITICAL)

```typescript
// api/entries.ts
submitForReview: (id: string) =>
  client.post(`/entries/${id}/submit`),  // was /review
```

### FE-04 · Fix `restoreVersion` contract (CRITICAL)

```typescript
// api/entries.ts
restoreVersion: (entryId: string, versionId: string) =>
  client.post(`/entries/${entryId}/versions/${versionId}/restore`),
// (now matches new backend route added in BE-03)
```

### FE-05 · Fix `siteId` hardcoded empty string in `ContentTypeEditPage.tsx` (CRITICAL)

```typescript
// ContentTypeEditPage.tsx — read from SiteContext
const { selectedSiteId } = useSite();

// In form submit:
await contentTypesApi.create({
  siteId: selectedSiteId,   // was: ''
  handle: data.apiKey,
  displayName: data.name,
  description: data.description,
});
```

Add guard: if `selectedSiteId` is null/undefined, show inline error "Please select a site before creating a content type."

### FE-06 · Fix pagination param name in `api/entries.ts`

```typescript
// api/entries.ts
export interface EntryListParams {
  siteId?: string;
  contentTypeId?: string;
  status?: EntryStatus;
  locale?: string;
  search?: string;
  folderId?: string;     // ADD
  pageNumber?: number;   // was: page
  pageSize?: number;
}
```

### FE-07 · ContentTypes page — card grid layout

**Current:** flat table. **Design:** card grid.

**Rework `ContentTypesPage.tsx`:**

- Replace `<table>` with a CSS grid (`grid-template-columns: repeat(auto-fill, minmax(300px, 1fr))`)
- `ContentTypeCard` component:
  - Emoji icon in a colored rounded square (color by hash of handle)
  - Name (bold) + description (muted)
  - Field-tag chips: show first 4 `field.handle` values, then `+N more` pill
  - Stats row: `{entryCount} entries · {fieldCount} fields · {localeCount} locales`
  - "Browse →" button + kebab menu (Edit / Archive / Delete)
  - Status badge
- Add dashed "Create Content Type" card at grid end
- Add search input + site filter dropdown to filter bar
- Add "Import Schema" secondary button in page header

### FE-08 · New Content Type modal — add missing fields

**Add to `ContentTypeEditPage.tsx` create modal:**

```typescript
// Extend Zod schema
const createSchema = z.object({
  name: z.string().min(1).max(200),
  apiKey: z.string().min(1).max(64).regex(/^[a-z0-9][a-z0-9-]*[a-z0-9]$/,
    'Lowercase letters, digits, hyphens only'),
  description: z.string().max(500).optional(),
  siteId: z.string().uuid('Please select a site'),
  template: z.enum(['Blank', 'BlogPost', 'Documentation', 'LandingPage']).default('Blank'),
  localizationMode: z.enum(['PerLocale', 'Shared']).default('PerLocale'),
});
```

Add to modal form:
- `<select>` for Site (populated from `useSite()` context — all sites for tenant)
- Two-column row: Template `<select>` + Localization Mode `<select>`
- API Key input with `api/v1/` prefix adornment (read-only prefix in input group)

### FE-09 · Entry List — folder tree sidebar

**Add to `EntriesPage.tsx`:**

```
[FolderTree 200px] | [EntriesPane flex-1]
```

`FolderTree` component:
- Static system folders: All Entries, Drafts, Pending Review, Archived (with counts from `stats`)
- Custom folders: loaded from `GET /sites/{siteId}/folders`
- Active folder highlighted; click sets `activeFolderId` state
- Each folder shows name + count badge
- Collapsible nested children (max 2 levels per design)
- "+ New Folder" button at bottom (inline input → `POST /sites/{siteId}/folders`)

Pass `activeFolderId` as filter to entries list query.

### FE-10 · Entry List — additional filters and columns

**Add to `EntriesPage.tsx`:**

**Filters (add to filter bar):**
- `LocaleFilter` `<select>` (populated from `GET /sites/{siteId}/locales` or hardcoded common locales)
- `AuthorFilter` `<select>` (populated from `GET /users?siteId=`)
- Export button (calls `GET /entries/export?...` and triggers file download)

**Table columns to add:**
- Locale column (flag + code — e.g., 🇺🇸 en-US)
- Version column (muted `v{n}`)
- Tag pills below the title cell (render `entry.fields.tags` if present as an array)

**Bulk selection:**
- Add `selectedIds: Set<string>` state
- Checkbox in header row (select all on page)
- Checkbox per row
- `BulkActionsBar` component (appears when `selectedIds.size > 0`):
  - "N entries selected."
  - Publish | Unpublish | Export | Delete buttons
  - Clear (×) button

**Per-page selector:** `<select>` with options 10/25/50/100, connected to `pageSize` state.

### FE-11 · Entry List — secondary tabs (Schema, Localization, API Preview)

Add tabs row below page header:

```
Entries (227) | Schema (9 fields) | Localization | API Preview
```

**Schema tab:**
- Renders `contentType.fields` as a sortable table (drag handle via `@dnd-kit/sortable`)
- Columns: drag handle, Field Name, API Key (code), Type (colored badge), Required, Localized, Indexed, Edit/Delete
- "Add Field" button → opens existing field-editor inline form
- "Export JSON Schema" button → downloads `{handle}-schema.json`
- Schema version badge (`v{n} · last edited by {user}`)

**Localization tab:**
- Fallback chain visual (en-US → fr-FR → de-DE → + Add)
- Coverage table: entry titles × locale columns, with per-cell status badge
- "✨ Batch translate missing" button (POST to AI endpoint — Phase 3)

**API Preview tab:**
- Table of REST endpoints (GET list, GET by id, POST, PUT, DELETE, GET versions)
- Sample JSON response (dark-themed `<pre>` block using entry DTO shape)
- GraphQL query example
- "Copy" and "Download OpenAPI spec" buttons

### FE-12 · Entry Editor — workflow stepper

**Add to right sidebar of `EntryEditorPage.tsx`:**

`WorkflowStepper` component:
```typescript
// Steps: Draft → PendingReview → Approved → Published
// Props: currentStatus, onTransition(action)
```
- Visual step indicators: completed (✓), active (filled), future (hollow)
- Action buttons below stepper:
  - Draft: "Submit for Review" → `entriesApi.submitForReview(id)`
  - PendingReview: "Approve" → `entriesApi.approve(id)` | "Reject" → opens rejection reason modal
  - Approved: "Publish Now" → `entriesApi.publish(id)` | "Schedule" → schedule date-time picker
  - Published: "Unpublish" → `entriesApi.unpublish(id)`

**Add `approve` and `reject` to `api/entries.ts`:**

```typescript
approve: (id: string) => client.post(`/entries/${id}/approve`),
reject: (id: string, reason: string) =>
  client.post(`/entries/${id}/reject`, { reason }),
unpublish: (id: string) => client.post(`/entries/${id}/unpublish`),
```

### FE-13 · Entry Editor — schedule publish UI

In the Approved step:
- "Schedule" button opens an inline date-time picker (`<input type="datetime-local">`)
- Confirm button calls `entriesApi.schedule(id, { publishAt, unpublishAt? })`
- Scheduled entry shows `Scheduled · publishes {relative time}` badge in topbar

```typescript
schedule: (id: string, req: { publishAt: string; unpublishAt?: string }) =>
  client.post(`/entries/${id}/schedule`, req),
cancelSchedule: (id: string) =>
  client.delete(`/entries/${id}/schedule`),
```

### FE-14 · Entry Editor — SEO panel

**Add `SeoPanel` component to right sidebar:**

```typescript
interface SeoPanelProps {
  entryId: string;
  initialSeo: SeoMetadata | null;
}
```

Fields:
- Meta Title: `<input>` with `{length}/60` char counter; turns amber >50, red >60
- Meta Description: `<textarea>` with `{length}/160` char counter
- SERP Preview block: renders a Google result mockup (title in blue, URL in green, description in grey)
- Canonical URL: `<input type="url">`
- OG Image: asset picker (opens MediaLibrary modal)

Auto-save on blur: calls `PUT /entries/{id}/seo` with `{ metaTitle, metaDescription, canonicalUrl, ogImage }`.

### FE-15 · Entry Editor — locale switcher pill buttons

**Current:** `<select>` dropdown. **Design:** pill button group.

```tsx
<div className="locale-switcher">
  {localeVariants.map(locale => (
    <button
      key={locale}
      className={`locale-pill ${activeLocale === locale ? 'active' : ''}`}
      onClick={() => setActiveLocale(locale)}
    >
      <FlagIcon locale={locale} />
      {locale}
      {locale === activeLocale && <StatusDot />}
    </button>
  ))}
  <button className="locale-pill add">+ Add locale</button>
</div>
```

Show a missing-translation warning badge (`⚠`) on locales without an entry variant.

### FE-16 · Entry Editor — typed field pickers

**Problem:** `AssetReference` and `Reference` (Entry Ref) fields render as plain text inputs. `Enum` field renders as plain text (no options). `TaxonomyRef` falls to default text input.

**Fixes in `FieldInput` component switch:**

```typescript
case 'AssetReference':
  return <AssetPickerField value={value} onChange={onChange}
    label={field.label} />;
  // Opens MediaLibrary modal on click; displays thumbnail + filename

case 'Reference':
  return <EntryPickerField value={value} onChange={onChange}
    label={field.label}
    contentTypeId={field.referenceContentTypeId} />;
  // Opens entry search modal filtered by ContentType

case 'Enum':
  return <select value={value} onChange={e => onChange(e.target.value)}>
    {field.options?.map(opt =>
      <option key={opt} value={opt}>{opt}</option>)}
  </select>;
  // FieldDefinitionDto needs an Options: string[] property

case 'Component':
  return <ComponentRefField value={value} onChange={onChange}
    componentId={field.componentId} />;

// Taxonomy — add new case:
case 'TaxonomyRef':
  return <TaxonomyTagField value={value} onChange={onChange}
    label={field.label} />;
  // Renders tag pills + type-ahead; calls GET /taxonomy/tags
```

### FE-17 · Entry Editor — Preview button

**Add to editor topbar:**

```typescript
// 1. Fetch preview token
const { data: tokenData } = useMutation(() =>
  entriesApi.getPreviewToken(id));

// 2. Open preview URL in new tab
<button onClick={async () => {
  const { token } = await getPreviewToken(id);
  window.open(`/entries/preview?token=${token}`, '_blank');
}}>Preview ↗</button>
```

**Add to `api/entries.ts`:**

```typescript
getPreviewToken: (id: string) =>
  client.get<{ token: string; expiresAt: string }>(`/entries/${id}/preview-token`),
```

### FE-18 · ContentType field editor — add `IsIndexed` toggle

In `ContentTypeEditPage.tsx` field rows, add an "Indexed" checkbox next to "Required" and "Localized".

Update field schema:

```typescript
const fieldSchema = z.object({
  id: z.string().optional(),
  name: z.string().min(1),
  apiKey: z.string().min(1),
  type: z.nativeEnum(FieldType),
  required: z.boolean(),
  localized: z.boolean(),
  isIndexed: z.boolean(),   // ADD
  isUnique: z.boolean(),    // ADD (already in FieldDefinitionDto but not in form)
});
```

---

## Part 4 — Implementation Sequence

### Sprint 1 — Critical Contract Fixes (Days 1–3)
All of these are silent runtime bugs or 404s that affect existing functionality.

| Task | Ref | Files |
|---|---|---|
| Add EF migration for SEO + FolderId + IsIndexed columns | DB-01, DB-02 | `*_AddEntrySeoPlusFolderAndIndexedField.cs` |
| Fix `FieldsJson` → `Fields` object in DTOs and handlers | BE-04 | `EntryDto`, `EntryListItemDto`, handlers |
| Fix `EntriesPage` pagination param name (`page` → `pageNumber`) | BE-06, FE-06 | `ListEntriesQuery`, `api/entries.ts` |
| Fix `submitForReview` route (`/review` → `/submit`) | BE-02, FE-03 | `api/entries.ts` |
| Add version restore REST route | BE-03, FE-04 | `EntriesController`, `api/entries.ts` |
| Fix `siteId: ''` in ContentType create | FE-05 | `ContentTypeEditPage.tsx` |
| Align `EntryStatus` enum names (`PendingApproval` → `PendingReview`) | BE-05, FE-02 | `EntryStatus.cs`, `types/index.ts`, all usages |

### Sprint 2 — Backend Completeness (Days 4–7)

| Task | Ref | Files |
|---|---|---|
| Add `EntryCount`, `LocaleCount` to `ContentTypeListItemDto` | BE-07a | `ListContentTypesQueryHandler` |
| Add `LocalizationMode` to ContentType entity + DB | DB-04 | entity, EF config, migration, DTOs |
| Add `IsIndexed` to `FieldDefinition` domain + DB | DB-02, BE-08 | entity, migration, commands |
| Add `Title`, `AuthorName`, `ContentTypeName` to `EntryListItemDto` | BE-01 | `ListEntriesQueryHandler` |
| Add `LocaleVariants` to `EntryDto` | BE-01 | `GetEntryQueryHandler` |
| Add `approve`, `reject`, `unpublish` backend status checks | — | `EntriesController`, domain |
| Add `EntryFolders` table + CRUD controller | DB-03, BE-09 | migration, `EntryFolder.cs`, controller |
| Add Import Schema endpoint | BE-07c | `ContentTypesController` |
| Allow hyphens in ContentType handle validator | BE-07d | `CreateContentTypeCommandValidator` |
| Add `options` to `FieldDefinitionDto` for Enum type | BE-07 | `FieldDefinitionDto`, field schema |

### Sprint 3 — Frontend ContentTypes UI (Days 8–10)

| Task | Ref | Files |
|---|---|---|
| Rework ContentTypes list to card grid | FE-07 | `ContentTypesPage.tsx`, new `ContentTypeCard` |
| Add search + site filter to ContentTypes page | FE-07 | `ContentTypesPage.tsx` |
| Add "Import Schema" button + modal | FE-07 | new `ImportSchemaModal` |
| Add Site, Template, LocalizationMode to Create modal | FE-08 | `ContentTypeEditPage.tsx` |
| Add API key prefix adornment + hyphen validation | FE-08 | `ContentTypeEditPage.tsx` |
| Add `IsIndexed` + `IsUnique` checkboxes to field editor | FE-18 | `ContentTypeEditPage.tsx` |

### Sprint 4 — Frontend Entries List (Days 11–14)

| Task | Ref | Files |
|---|---|---|
| Add folder tree sidebar panel | FE-09 | `EntriesPage.tsx`, new `FolderTree` component |
| Add locale filter + author filter to filter bar | FE-10 | `EntriesPage.tsx` |
| Add locale + version columns to entries table | FE-10 | `EntriesPage.tsx` |
| Add bulk selection checkboxes + `BulkActionsBar` | FE-10 | `EntriesPage.tsx`, new `BulkActionsBar` |
| Add per-page selector | FE-10 | `EntriesPage.tsx` |
| Add Export button | FE-10 | `EntriesPage.tsx`, `api/entries.ts` |
| Add Schema tab (sortable field table) | FE-11 | `EntriesPage.tsx`, new `SchemaTab` |
| Add Localization tab (coverage grid) | FE-11 | `EntriesPage.tsx`, new `LocalizationTab` |
| Add API Preview tab | FE-11 | `EntriesPage.tsx`, new `ApiPreviewTab` |

### Sprint 5 — Frontend Entry Editor (Days 15–19)

| Task | Ref | Files |
|---|---|---|
| Replace locale `<select>` with pill button group | FE-15 | `EntryEditorPage.tsx` |
| Add `WorkflowStepper` to sidebar | FE-12 | `EntryEditorPage.tsx`, new `WorkflowStepper` |
| Add approve/reject/unpublish to `api/entries.ts` | FE-12 | `api/entries.ts` |
| Add Schedule publish UI (datetime picker + API call) | FE-13 | `EntryEditorPage.tsx`, `api/entries.ts` |
| Add `SeoPanel` to sidebar | FE-14 | `EntryEditorPage.tsx`, new `SeoPanel` |
| Add Preview button + token fetch | FE-17 | `EntryEditorPage.tsx`, `api/entries.ts` |
| Replace AssetReference plain text with `AssetPickerField` | FE-16 | new `AssetPickerField` (opens MediaPage modal) |
| Replace Reference plain text with `EntryPickerField` | FE-16 | new `EntryPickerField` (search modal) |
| Replace Enum plain text with `<select>` using options | FE-16 | `EntryEditorPage.tsx` |
| Add TaxonomyRef tag picker | FE-16 | new `TaxonomyTagField` |

---

## Security Checklist

- [ ] `ContentTypeEditPage`: guard against empty `siteId` before submit — show inline error, never send `''` to API (FE-05)
- [ ] `ContentTypesController.Delete`: return 409 if entries exist for this content type (BE-10)
- [ ] `EntriesController.BulkDelete`: require explicit confirmation token to prevent accidental mass delete (BE-10)
- [ ] `EntriesController.Schedule`: validate `publishAt > UtcNow` server-side — do not trust client clock (BE-10)
- [ ] `ImportSchema`: validate against a strict schema shape; cap at 50 fields; run under `ContentAdmin` role only (BE-10)
- [ ] Preview token endpoint: tokens must be short-lived (15 min TTL) and single-use (BE-10)
- [ ] Entry restore/rollback: only the original author or a `ContentAdmin` may restore — enforce in handler, not just controller
- [ ] Reject reason text: sanitize free-text input (strip HTML) before storing
- [ ] Bulk actions: enforce same per-entry authorization checks on each ID in the batch (no shortcut)
- [ ] `FieldsJson` deserialization: cap document size (e.g., 1 MB) before parsing to prevent memory exhaustion on large payloads

---

## File Change Index

| File | Change Type | Sprint |
|---|---|---|
| `Migrations/*_AddEntrySeoPlusFolderAndIndexedField.cs` | Create | 1 |
| `Domain/Entries/EntryStatus.cs` | Rename enum value | 1 |
| `Application/Entries/DTOs/EntryDto.cs` | Add Fields dict, remove FieldsJson | 1 |
| `Application/Entries/DTOs/EntryListItemDto.cs` | Add Title, AuthorName, ContentTypeName | 1,2 |
| `Application/Entries/Queries/ListEntriesQueryHandler.cs` | Add joins, rename param | 1,2 |
| `Application/Entries/Queries/GetEntryQueryHandler.cs` | Add LocaleVariants | 2 |
| `Application/Entries/Commands/UpdateEntryCommandHandler.cs` | Accept dict, serialize | 1 |
| `Application/Entries/Commands/CreateEntryCommandHandler.cs` | Accept dict, serialize | 1 |
| `API/Controllers/EntriesController.cs` | Add restore route, fix param names | 1,2 |
| `Domain/ContentTypes/ContentType.cs` | Add LocalizationMode | 2 |
| `Domain/ContentTypes/FieldDefinition.cs` | Add IsIndexed | 2 |
| `Application/ContentTypes/DTOs/ContentTypeListItemDto.cs` | Add EntryCount, LocaleCount | 2 |
| `Application/ContentTypes/DTOs/FieldDefinitionDto.cs` | Add IsIndexed, Options | 2 |
| `Application/ContentTypes/Validators/*` | Allow hyphens in handle | 2 |
| `API/Controllers/ContentTypesController.cs` | Add import endpoint | 2 |
| `API/Controllers/EntryFoldersController.cs` | Create new | 2 |
| `clientApp/src/types/index.ts` | Fix EntryStatus, add missing fields | 1 |
| `clientApp/src/api/entries.ts` | Fix routes, params, add new calls | 1,2 |
| `clientApp/src/pages/ContentTypesPage.tsx` | Card grid + filters + import | 3 |
| `clientApp/src/pages/ContentTypeEditPage.tsx` | Fix siteId, add modal fields | 1,3 |
| `clientApp/src/pages/EntriesPage.tsx` | Folder tree, filters, bulk, tabs | 4 |
| `clientApp/src/pages/EntryEditorPage.tsx` | Workflow, SEO, schedule, pickers | 5 |
| `clientApp/src/components/WorkflowStepper.tsx` | Create new | 5 |
| `clientApp/src/components/SeoPanel.tsx` | Create new | 5 |
| `clientApp/src/components/FolderTree.tsx` | Create new | 4 |
| `clientApp/src/components/BulkActionsBar.tsx` | Create new | 4 |
| `clientApp/src/components/AssetPickerField.tsx` | Create new | 5 |
| `clientApp/src/components/EntryPickerField.tsx` | Create new | 5 |
| `clientApp/src/components/TaxonomyTagField.tsx` | Create new | 5 |
| `clientApp/src/components/SchemaTab.tsx` | Create new | 4 |
| `clientApp/src/components/ApiPreviewTab.tsx` | Create new | 4 |
| `clientApp/src/components/ContentTypeCard.tsx` | Create new | 3 |

---

*This plan covers ContentTypes and Entries in full. See `IMPLEMENTATION_PLAN.md` for the broader feature roadmap including Sites, AI Copilot, and Page Designer.*
