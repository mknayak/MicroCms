# ADR-005: Component Backing Content Type

**Status:** Accepted  
**Date:** 2025-01  
**Deciders:** Engineering team  
**Files affected:**
- `src/MicroCMS.Domain/Aggregates/Components/Component.cs`
- `src/MicroCMS.Application/Features/Components/Services/ComponentBackingTypeProvisioner.cs`
- `src/MicroCMS.Application/Features/Components/Handlers/ComponentHandlers.cs`
- `src/MicroCMS.Application/DependencyInjection.cs`

---

## Context

The `Component` aggregate defines the *shape* of a reusable UI element (Hero Banner, CTA
block, etc.) including its fields. `ComponentItem` stores *instances* of component data
(the actual field values).

Before this decision, component items were stored as raw `FieldsJson` blobs. This worked
but meant that:

1. Component item data was opaque to the query pipeline — no field-level search or
   validation against a schema.
2. Adding a new field to a component and migrating existing items required a bespoke
   migration with no shared infrastructure.
3. The Delivery API had to treat component data as an untyped JSON payload.

---

## Decision

When a `Component` is created, **automatically provision a shadow `ContentType`** with
`Kind = Component` that mirrors the component's field schema. The component holds a
reference to this backing type via `BackingContentTypeId`.

### Provisioner

`ComponentBackingTypeProvisioner` (Application service, scoped):

```csharp
public async Task ProvisionAsync(Component component, CancellationToken ct)
{
    var handle = $"__comp_{component.Key.Replace("-", "_")}";
    var contentType = ContentType.Create(
        component.TenantId, component.SiteId, handle,
   $"{component.Name} (Component Data)", ...,
        kind: ContentTypeKind.Component);
    await contentTypeRepo.AddAsync(contentType, ct);
  component.SetBackingContentType(contentType.Id);
    componentRepo.Update(component);
    await unitOfWork.SaveChangesAsync(ct);
}
```

### Naming convention

Backing type handles are prefixed with `__comp_` (double underscore) so they:
- Sort to the bottom of any alphabetical list.
- Can be reliably filtered out of the UI content type list.
- Are identifiable programmatically without a separate flag.

### Immutability of Component kind

`ContentTypeKind.Component` cannot be changed via `SetKind()`. This prevents a backing
type from accidentally becoming a user-facing content type.

### Sync responsibility

Field synchronisation between the `Component`'s own field list and the backing
`ContentType`'s fields is **not automatic** in the initial implementation.
`UpdateComponentCommandHandler` calls `comp.ReplaceFieldsFromData()` on the component;
a future event handler should sync the backing type fields accordingly.

---

## Consequences

**Positive:**
- Component item data can use the same validation, search, and migration infrastructure
  as regular content entries.
- The Delivery API can resolve component data the same way it resolves entry fields.
- A single `ContentTypeKind` flag keeps the abstraction leak minimal.

**Negative:**
- Every component creation now performs two DB writes (component + backing type).
- The sync gap between component fields and backing type fields must be closed in a
  follow-up (see future work below).
- The double-underscore naming convention is a convention, not a constraint — it could
  be violated by a future developer.

### Future work

- Event handler: `ComponentFieldsUpdatedEvent` → sync backing `ContentType` fields.
- Migration: back-fill `BackingContentTypeId` on existing components.

---

## Alternatives Considered

| Alternative | Rejected because |
|---|---|
| Keep raw `FieldsJson` on `ComponentItem` | No schema validation, no search |
| Separate `ComponentDataSchema` entity | More entities, more joins, no code reuse of `ContentType` infrastructure |
| Use EF-owned entity for component fields | Tight coupling to ORM; harder to version |
