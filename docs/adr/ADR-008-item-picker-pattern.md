# ADR-008: Item Picker Modal Pattern

**Status:** Accepted  
**Date:** 2025-01  
**Deciders:** Engineering team  
**Files affected:**
- `src/MicroCMS.Admin.WebHost/ClientApp/src/components/ItemPickerModal.tsx`
- `src/MicroCMS.Admin.WebHost/ClientApp/src/api/items.ts`
- `src/MicroCMS.Admin.WebHost/ClientApp/src/types/index.ts` (`ItemPickerResult`, `ItemPickerParams`)

---

## Context

In the Page Designer, a user places a component into a zone and then **binds a content
item** to that placement (e.g., placing a "Hero Banner" component and selecting a specific
"Summer Sale" hero banner item). This binding is stored as `BoundItemId` on the placement.

The selection UI needs to:
- Filter items by the component's backing content type.
- Support text search.
- Filter by publication status (`Draft`, `Published`, `Archived`, `All`).
- Support pagination (content libraries can have hundreds of items).
- Be reusable from both the Page Designer and future list-type field pickers.

---

## Decision

Implement a **modal overlay component** (`ItemPickerModal`) that is rendered into the
document root (full-screen overlay) and communicates the selection via a callback prop.

### Component API

```typescript
interface ItemPickerModalProps {
  contentTypeId: string;      // filter items to this backing type
  componentName: string;      // display name shown in the modal header
  onSelect: (item: ItemPickerResult) => void;
  onClose: () => void;
}
```

### Data model

```typescript
interface ItemPickerResult {
  id: string;
  title: string;
  status: 'Draft' | 'Published' | 'Archived';
  updatedAt: string;
  contentTypeId: string;
}
```

### Search API endpoint

`GET /component-items/search` with query params:

```typescript
interface ItemPickerParams {
  contentTypeId: string;
  search?: string;
  status?: 'Draft' | 'Published' | 'Archived';
  page?: number;
  pageSize?: number;
}
```

Returns `PagedResult<ItemPickerResult>`.

### UX decisions

| Decision | Rationale |
|---|---|
| **Full-screen overlay**, not a slide-in panel | Prevents layout reflow in the Page Designer canvas |
| **Pagination size: 10** | Balances "enough items visible" vs. page load time |
| **Stale time: 30 s** on TanStack Query | Results are unlikely to change in the time between opening and closing the picker |
| **Status filter pills**, not a dropdown | Tabs/pills are faster to interact with for a common filter like status |
| Click row OR "Select" button both trigger selection | Users expect both patterns from other CMS pickers (Contentful, Sanity) |
| **No item creation** inside the picker | Keeps the picker focused; users create items in the Component Items section |

### Reusability

`ItemPickerModal` is a generic component. Future uses:
- Reference field picker (bind an entry to a `Reference` field).
- Media picker (with `contentTypeId` replaced by media folder filter).
- Collection picker for page route binding.

The modal does not contain any Page Designer-specific logic; it communicates purely via
`onSelect` / `onClose` callbacks.

---

## Consequences

**Positive:**
- Consistent item-selection UX across the application via a single reusable component.
- Server-side filtering and pagination scale to large item libraries without degrading
  browser performance.
- Modal overlay keeps the Page Designer canvas state intact while the picker is open.

**Negative:**
- Mounting the overlay at document root requires either a React portal or CSS z-index
  management. The current implementation uses a fixed-positioned overlay with `z-50`
  (Tailwind) — sufficient but not a formal portal.
- Users cannot create a new item inline; they must navigate away, losing their place in
  the designer. A future "Quick Create" flow inside the picker could address this.

---

## Alternatives Considered

| Alternative | Rejected because |
|---|---|
| Inline dropdown in the Page Designer placement card | Cannot display search + pagination in a small dropdown |
| Slide-in drawer panel | Partially obscures the canvas; less common pattern in CMS tools |
| Navigate to a dedicated `/items/pick` route | Loses Page Designer state unless persisted to session storage |
| Load all items client-side and filter in memory | Does not scale; components can have thousands of items |
