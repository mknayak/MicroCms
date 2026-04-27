# ADR-009: Three-Tier Layout → Template → Page Hierarchy

**Status:** Accepted  
**Date:** 2025-01  
**Supersedes:** Portions of ADR-001 (Page Designer sidebar pattern)  
**Deciders:** Engineering team  
**Files affected:**
- `src/MicroCMS.Domain/Aggregates/Components/SiteTemplate.cs` *(new)*
- `src/MicroCMS.Shared/Ids/SiteTemplateId.cs` *(new)*
- `src/MicroCMS.Domain/Aggregates/Pages/Page.cs` — added `SiteTemplateId`
- `src/MicroCMS.Infrastructure/Persistence/Common/ApplicationDbContext.cs`
- `src/MicroCMS.Infrastructure/Persistence/Common/Configurations/SiteTemplateConfiguration.cs` *(new)*
- `src/MicroCMS.Infrastructure/DependencyInjection.cs`
- `src/MicroCMS.Admin.WebHost/ClientApp/src/api/siteTemplates.ts` *(new)*
- `src/MicroCMS.Admin.WebHost/ClientApp/src/pages/page-templates/PageTemplatesPage.tsx` *(new)*
- `src/MicroCMS.Admin.WebHost/ClientApp/src/pages/page-templates/PageTemplateDesignerPage.tsx` *(new)*
- `src/MicroCMS.Admin.WebHost/ClientApp/src/pages/designer/PageDesignerPage.tsx` — refactored
- `src/MicroCMS.Admin.WebHost/ClientApp/src/pages/pages/ChildCard.tsx` — added Design button
- `src/MicroCMS.Admin.WebHost/ClientApp/src/pages/pages/PagesPage.tsx` — added Design button
- `src/MicroCMS.Admin.WebHost/ClientApp/src/components/layout/Sidebar.tsx` — renamed
- `src/MicroCMS.Admin.WebHost/ClientApp/src/App.tsx` — updated routes

---

## Context

The original Page Designer was a full-screen sidebar app where:

1. **Zones were hardcoded** (`header-zone`, `hero-zone`, `content-zone`, `cta-zone`, `footer-zone`) regardless of what zones the page's assigned layout defined.
2. **The designer was a disconnected global page** in the sidebar. Clicking "Page Designer" opened a page-tree selector, which felt disconnected from the Pages section.
3. **There was no template tier.** With 100 pages, you would have to manually add a Header component and Footer component to every page individually. There was no way to say "all blog pages share a standard header/footer".

---

## Decision

Introduce a **three-tier hierarchy**:

```
Layout (zones)
  └── Site Template (shared component placements)
        └── Page (page-specific component placements)
```

### Tier 1: Layout

Unchanged from ADR-001. Defines the **zone structure** (header, content, footer, grid rows).
Lives at `/layouts` → "Design Layout" button.

### Tier 2: Site Template (new)

A `SiteTemplate` is a reusable, named template:
- **Linked to one Layout** — so it knows which zones exist.
- **Stores a set of component placements** (e.g., a Global Nav component in `header`, a Global Footer in `footer`).
- **Reused by many pages** — a page opts in to a template via `Page.SiteTemplateId`.
- Managed at `/page-templates` (formerly "Page Designer" sidebar item).

**Domain model:**
```csharp
public sealed class SiteTemplate : AggregateRoot<SiteTemplateId>
{
    public LayoutId LayoutId { get; }       // which layout's zones apply
    public string Name { get; }
    public string PlacementsJson { get; }   // shared component placements
}
```

**Page link:**
```csharp
// Page aggregate
public SiteTemplateId? SiteTemplateId { get; private set; }
public void SetSiteTemplate(SiteTemplateId? id) { ... }
```

### Tier 3: Page

A page's designer (`/pages/:id/designer`) shows:
1. **Inherited placements** from its linked `SiteTemplate` — rendered with an "inherited" badge, cannot be removed in the page designer.
2. **Page-specific placements** — freely added/edited by the editor.

Zones shown on the canvas are read from the page's assigned layout, **not hardcoded**.

### Sidebar rename

| Before | After |
|--------|-------|
| "Page Designer" → `/designer` | "Page Templates" → `/page-templates` |

The old `/designer` route redirects to `/pages` to avoid broken bookmarks.

### Page Designer navigation

Page-specific designing is **no longer accessible from the sidebar**. The flow is:

```
Pages list → [Design] button on page card → Page Designer for that page → Save → back to Pages
```

Each page card now shows two action buttons: **Edit** (opens the detail panel) and **Design** (navigates to `/pages/:id/designer`).

---

## Consequences

**Positive:**
- 100 pages can share one template → change the Header once and all 100 pages update.
- Zones are driven by the layout, not hardcoded → no more `header-zone` mismatch.
- Page Designer is contextual (originates from the page) — no disconnected sidebar list.
- Clear mental model: Layout (structure) → Template (shared content) → Page (unique content).

**Negative:**
- Adds a third entity (`SiteTemplate`) that authors must understand.
- A page not linked to any template has no inherited placements — the experience is the same as before, which is acceptable.
- Template changes immediately propagate to all linked pages on the next render — no per-page opt-out. A future "override" mechanism could address this.

### Future work

- `PUT /pages/{id}/site-template` API endpoint to link/unlink a page to a template.
- Template inheritance preview: show in Pages list which template each page uses.
- Template versioning: allow a page to be pinned to a specific template version.

---

## Alternatives Considered

| Alternative | Rejected because |
|---|---|
| Layout-level default placements only (no template tier) | Cannot have *multiple* templates per layout (e.g., "Blog Template" vs "Landing Template" both using the same layout) |
| Copy-paste template: create a page and "duplicate" it | No inheritance — changing the header still requires updating every copied page |
| Global header/footer as layout zones only | Mixing zone structure (Layout concern) with component placement (Template/Page concern) |
