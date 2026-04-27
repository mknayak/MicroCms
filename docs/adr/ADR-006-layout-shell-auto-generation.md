# ADR-006: Layout Shell Auto-Generation

**Status:** Accepted  
**Date:** 2025-01  
**Deciders:** Engineering team  
**Files affected:**
- `src/MicroCMS.Application/Features/Layouts/Services/LayoutShellGeneratorService.cs`
- `src/MicroCMS.Application/Features/Layouts/Handlers/LayoutHandlers.cs`
- `src/MicroCMS.Domain/Aggregates/Components/Layout.cs`

---

## Context

A layout's **shell template** is the complete HTML document (including `<head>`, `<body>`,
and zone placeholder tokens) that the rendering engine uses to produce the final HTML for
a page.

Two sources of truth existed before this decision:

1. **Zone definitions** — the named regions declared on the layout (Header, Content, Footer).
2. **Shell template** — the raw HTML the user typed, which was expected to contain tokens
   matching the zone names.

These could diverge. A zone renamed in the designer would not update the shell, silently
breaking that zone's rendering.

---

## Decision

The shell template is no longer editable by the user. It is **computed from the zone tree**
by `LayoutShellGeneratorService` every time zones are saved, and stored on `Layout.ShellTemplate`
as a server-managed field.

### Generation algorithm

**Input:** `zonesJson` (stored on `Layout.ZonesJson`) + `templateType` (`Handlebars` | `Html`)

**Output:** A complete HTML document string.

#### Handlebars output example (zone named `hero-zone`)
```handlebars
<div data-zone="hero-zone">
  {{{hero_zone}}}
</div>
```
Hyphens in zone names are replaced with underscores. Triple-stash (`{{{ }}}`) is used to
emit unescaped HTML (required for component output).

#### Html output example
```html
<div data-zone="hero-zone">
  {{zone:hero-zone}}
</div>
```
The `{{zone:name}}` token uses the original zone name with hyphens preserved.

#### Grid row output (both engines)
```html
<div class="grid-row" data-zone-row="grid-abc123">
  <div class="col-6" data-zone="grid-abc123-col-6-0">
    <!-- zone token -->
  </div>
  <div class="col-6" data-zone="grid-abc123-col-6-1">
    <!-- zone token -->
  </div>
</div>
```

#### SEO tokens (always emitted in `<head>`)

| Engine | Title token | Description token |
|--------|------------|-------------------|
| Handlebars | `{{seo_title}}` | `{{seo_description}}` |
| Html | `{{seo:title}}` | `{{seo:description}}` |

### When generation fires

`LayoutShellGeneratorService.Generate()` is called from:
- `CreateLayoutCommandHandler` — on initial creation (default zones)
- `UpdateLayoutZonesCommandHandler` — whenever zones are changed via the designer

The generated shell is stored via `layout.SetGeneratedShell(shell)`.

### Advanced override (future)

A future `PUT /layouts/{id}/shell` endpoint could allow power users to supply a custom
shell, bypassing generation. The `ShellTemplate` property would be marked as
`IsCustomShell = true` and excluded from future auto-regeneration.

---

## Consequences

**Positive:**
- Zone tokens in the shell and the Page Designer are always in sync.
- Users never need to write HTML to define a layout.
- The generated shell includes `data-zone` attributes, enabling CSS-based layout
  visualisation in the designer preview.

**Negative:**
- Custom shell templates (e.g., inserting a third-party analytics script in `<head>`)
  require either the future override endpoint or a post-process step.
- The generator produces structural HTML only — no CSS, no custom attributes. Visual
  polish is the responsibility of the site's stylesheet.

---

## Alternatives Considered

| Alternative | Rejected because |
|---|---|
| Live preview that validates tokens | Doesn't eliminate the divergence — user can still save a broken template |
| Code mirror / syntax-highlighted textarea with token autocomplete | Better UX, but still user-authored; tokens can still be deleted manually |
| Store zones and shell independently, validate on save | Two sources of truth for the same data; validation would be lossy |
