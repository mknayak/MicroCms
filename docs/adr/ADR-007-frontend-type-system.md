# ADR-007: Centralised Frontend Type Contract

**Status:** Accepted  
**Date:** 2025-01  
**Deciders:** Engineering team  
**Files affected:**
- `src/MicroCMS.Admin.WebHost/ClientApp/src/types/index.ts`

---

## Context

The Admin SPA was growing a scattered collection of inline type definitions, duplicated
`interface` declarations across feature folders, and mutable type names that drifted away
from the backend DTO names. Specific problems included:

1. `ContentType` in the frontend had a different field structure from `ContentTypeDto`
   on the backend, causing silent deserialisation bugs.
2. `LayoutListItem` was missing `zones`-related fields even after the backend added them.
3. `FieldType`, `ContentTypeKind`, `LayoutTemplateType` were redeclared in multiple files
   with slightly different spellings.
4. Lock-related types (`EditLock`, `AcquireLockRequest`) had no frontend equivalent.

---

## Decision

All API-contract types live in **a single file**: `src/types/index.ts`.

### Principles

1. **Mirror the backend DTO name** — backend `ContentTypeDto` → frontend `ContentType` with a comment `// Matches ContentTypeDto`.
2. **Enums as string unions** — TypeScript string union types (e.g., `type FieldType = 'ShortText' | 'LongText' | ...`) rather than TypeScript enums, avoiding the compiled enum object overhead and matching how JSON values arrive.
3. **No inline types in API client files** — `api/*.ts` files import from `@/types`, never declare their own interfaces.
4. **Readonly semantics via `interface`** — mutable request shapes are `interface CreateXxxRequest { ... }`; server response shapes follow the same.
5. **Section comments** separate major domains: Auth, Tenant, ContentTypes, Entries, Media, Taxonomy, Users, Pages, Layouts, Component System, Locks, Item Picker.

### Types added in this phase

| Type | Mirrors backend |
|---|---|
| `ContentTypeKind` | `ContentTypeKind` enum |
| `LayoutZoneNode` | `LayoutZoneNodeDto` |
| `LayoutColumnDef` | `LayoutColumnDefDto` |
| `LayoutDefaultPlacement` | `LayoutDefaultPlacementDto` |
| `LayoutDto` (extended) | `LayoutDto` (with `zones[]`, `defaultPlacements[]`) |
| `CreateLayoutRequest` | `CreateLayoutCommand` |
| `UpdateLayoutRequest` | `UpdateLayoutCommand` |
| `UpdateLayoutZonesRequest` | `UpdateLayoutZonesCommand` |
| `UpdateLayoutDefaultPlacementsRequest` | `UpdateLayoutDefaultPlacementsCommand` |
| `EditLock` | `EditLockDto` |
| `AcquireLockRequest` | `AcquireLockCommand` |
| `LockEntityType` | `string` discriminator |
| `SavePlacementNode` | `SavePlacementNode` (recursive) |
| `ItemPickerResult` | `ComponentItemDto` (projected) |
| `ItemPickerParams` | query parameter bag |
| `PagedResult<T>` | `PagedList<T>` |

### Recursive types

`SavePlacementNode` is recursive (grid-row columns contain child placements).
TypeScript handles this via optional `columns` array:

```typescript
export interface SavePlacementNode {
  type: 'component' | 'grid-row';
  zone: string;
  sortOrder: number;
  componentId?: string;
  boundItemId?: string;
  isLayoutDefault?: boolean;
  columns?: Array<{
    span: number;
    zoneName: string;
    placements: SavePlacementNode[];
  }>;
}
```

---

## Consequences

**Positive:**
- Single source of truth — updating a DTO shape requires one change in one file.
- TypeScript catches backend/frontend contract drift at compile time.
- Easier code review — reviewers know where to look for type changes.

**Negative:**
- `types/index.ts` will grow large over time. If it exceeds ~800 lines per domain area,
  consider splitting into `types/content.ts`, `types/layout.ts`, etc. with a barrel
  re-export in `types/index.ts`.
- Importing `@/types` everywhere creates a monolithic dependency that can slow TypeScript
  Language Server incremental compilation on very large projects.

---

## Alternatives Considered

| Alternative | Rejected because |
|---|---|
| Auto-generate types from OpenAPI spec (e.g., `openapi-typescript`) | Backend API docs are not yet fully annotated; generation would produce incomplete types |
| Co-locate types with feature components | Leads to duplication when types are shared between features |
| Use `zod` schemas as the type source | Good option for validated forms; too heavy for read-only DTO shapes |
