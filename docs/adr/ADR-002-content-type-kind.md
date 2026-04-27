# ADR-002: Content Type Kind Discriminator

**Status:** Accepted  
**Date:** 2025-01  
**Deciders:** Engineering team  
**Files affected:**
- `src/MicroCMS.Domain/Aggregates/Content/ContentType.cs`
- `src/MicroCMS.Application/Features/ContentTypes/Commands/ContentTypeCommands.cs`
- `src/MicroCMS.Application/Features/ContentTypes/Dtos/ContentTypeDtos.cs`
- `src/MicroCMS.Application/Features/ContentTypes/Mappers/ContentTypeMapper.cs`
- `src/MicroCMS.Application/Features/ContentTypes/Handlers/ContentTypeCommandHandlers.cs`
- `src/MicroCMS.Api/Controllers/ContentTypesController.cs`
- `src/MicroCMS.Admin.WebHost/ClientApp/src/pages/content-types/ContentTypeEditPage.tsx`

---

## Context

MicroCMS has three distinct categories of structured content that share the same
`ContentType` entity but behave differently at runtime:

1. **Standard content** (blog post, product) — headless entries, consumed via Delivery API.
2. **Page content** — an entry that represents a site page; when an author creates one, a
   page-creation wizard is triggered to associate it with a URL, layout, and page template.
3. **Component backing types** — auto-created shadow types that give Components their own
   structured data store; never exposed in the content type list UI.

Before this decision these three cases were handled by separate flags (`isPage`, `isComponent`)
scattered across the codebase, leading to ambiguous logic and impossible state combinations
(e.g., `isPage = true AND isComponent = true`).

---

## Decision

Introduce a `ContentTypeKind` enum on the `ContentType` aggregate:

```csharp
public enum ContentTypeKind
{
    Content= 0,   // standard headless content
    Page      = 1,   // triggers page wizard on entry creation
    Component = 2,   // auto-created; not user-visible
}
```

### Rules enforced in the domain

| Transition | Allowed? | Reason |
|---|---|---|
| `Content` → `Page` | ✅ | Author promotes a type to be page-linked |
| `Page` → `Content` | ✅ | Author demotes; `LayoutId` is cleared automatically |
| `* ` → `Component` | ❌ | Component kind is set only at creation by `ComponentBackingTypeProvisioner` |
| `Component` → `*` | ❌ | Immutable; enforced by `SetKind()` domain guard |

### Page-kind: LayoutId

A `Page`-kind content type may optionally carry a `LayoutId` — the default layout applied
when the page wizard creates a new page. It is cleared automatically when the type is
demoted back to `Content`.

```csharp
public void SetLayout(LayoutId? layoutId)
{
    if (Kind != ContentTypeKind.Page)
      throw new BusinessRuleViolationException(...);
    LayoutId = layoutId;
}
```

### API surface

`POST /content-types` accepts `kind` (default `"Content"`).  
`PUT /content-types/{id}` accepts `kind` and `layoutId`.  
`PUT /content-types/{id}/layout` is a dedicated endpoint for layout-only changes.

### UI

`ContentTypeEditPage` shows a **Page Settings tab** (edit-only) containing:
- A kind radio (`Content` / `Page`)
- A layout dropdown (visible only when kind = `Page`)
- An explanatory callout describing the page wizard flow

`Component`-kind types are filtered out of the content type list on the frontend.

---

## Consequences

**Positive:**
- Single source of truth for "what is this type?", replacing multiple boolean flags.
- Domain guards prevent invalid state transitions.
- UI can be context-sensitive without complex boolean logic.

**Negative:**
- Existing data requires a migration to populate `Kind = Content` (default `0` means no
  migration is needed if the column defaults to `0`).
- The `Component` kind creates a coupling between the Component and ContentType aggregates.

---

## Alternatives Considered

| Alternative | Rejected because |
|---|---|
| Separate `PageContentType` and `ComponentContentType` entities | High duplication; fields, lifecycle, and indexing logic is shared |
| Boolean flags `IsPage` + `IsComponentBacking` | Allows impossible states; harder to extend with future kinds (e.g., `Form`) |
| Store kind only on the entry, not the content type | Kind needs to be known at *type definition* time, not just at entry creation |
