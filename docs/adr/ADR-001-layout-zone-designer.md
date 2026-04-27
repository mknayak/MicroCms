# ADR-001: Layout Zone Designer Architecture

**Status:** Accepted  
**Date:** 2025-01  
**Deciders:** Engineering team  
**Files affected:**
- `src/MicroCMS.Domain/Aggregates/Components/Layout.cs`
- `src/MicroCMS.Application/Features/Layouts/`
- `src/MicroCMS.Admin.WebHost/ClientApp/src/pages/layouts/LayoutDesignerPage.tsx`
- `src/MicroCMS.Admin.WebHost/ClientApp/src/pages/layouts/LayoutsPage.tsx`

---

## Context

Layouts define the HTML shell that wraps a rendered page. Before this decision, the shell
template was **hand-authored by the user** as raw HTML with zone tokens (`{{zone:header}}`).
This created three problems:

1. Users needed to know Handlebars/HTML token syntax to build layouts.
2. Zone names in the shell template could silently diverge from zone names used in the
   Page Designer, causing broken renders with no error feedback.
3. The shell template field on the edit form was a large free-text textarea with poor UX.

---

## Decision

Introduce a **visual Zone Designer** (three-panel IDE-like layout) and make the shell template
a **server-computed artefact** that is auto-regenerated whenever zones change.

### Zone Data Model

Zones are stored as a JSON tree on `Layout.ZonesJson`. Each node is either:

| `type`     | Meaning | Key fields |
|------------|---------|------------|
| `zone`| A single named zone — becomes one `data-zone` div | `name`, `label`, `sortOrder` |
| `grid-row` | A responsive grid row containing N column-zones | `name`, `columns[]` (each with `span` + `zoneName`) |

Grid columns use a 12-column system. Column `zoneName` values are derived from the parent
grid name (`{parentName}-col-{span}-{idx}`) and are stable — renaming the label does not
change the token.

### Shell Generation

The `LayoutShellGeneratorService` (Application layer) generates the shell from the zone JSON
each time zones are saved:

- **Handlebars:** `{{{zone_name}}}` (triple-stash, hyphens → underscores, unescaped HTML)
- **Html:** `{{zone:zone-name}}` (double-brace colon syntax)

SEO tokens (`{{seo_title}}` / `{{seo:title}}`) are always emitted in `<head>`.

### Default Placements

`Layout.DefaultPlacementsJson` stores a list of component placements that are pre-populated
on every page using this layout. Pages may override or add to these placements.

### UI Change

The `LayoutsPage` **create/edit form no longer shows a shell template textarea**. The form
only captures `name`, `key`, and `templateType`. After creation, a "Design Layout" button
in the row navigates to `/layouts/:id/designer`.

---

## Consequences

**Positive:**
- Zone tokens in the shell and Page Designer are always in sync — they share one source.
- Non-technical users can build structured layouts visually.
- Removing a zone from the designer immediately reflects in the regenerated shell.

**Negative:**
- Advanced users who want a fully custom shell must edit the auto-generated output externally
  (e.g., via the Management API or future "Edit Shell" advanced mode).
- The zone JSON adds a serialisation round-trip on every layout save.

---

## Alternatives Considered

| Alternative | Rejected because |
|---|---|
| Keep free-text textarea, add a live preview | Doesn't solve the token-divergence problem |
| Use a drag-and-drop library (react-beautiful-dnd) | Adds a heavy dependency; simple move-up/move-down is sufficient for zone ordering |
| Store zones as a separate DB table | Unnecessary complexity — zones are always queried with the layout; JSON column is simpler |
