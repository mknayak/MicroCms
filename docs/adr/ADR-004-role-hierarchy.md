# ADR-004: Six-Level Role Hierarchy

**Status:** Accepted  
**Date:** 2025-01  
**Deciders:** Engineering team  
**Files affected:**
- `src/MicroCMS.Application/Common/Authorization/Roles.cs`
- `src/MicroCMS.Application/Common/Authorization/RolePermissions.cs`
- `src/MicroCMS.Application/Common/Authorization/ContentPolicies.cs`

---

## Context

MicroCMS originally shipped with five roles derived from a generic CMS template:
`TenantAdmin`, `Editor`, `Approver`, `Author`, `Viewer`.

Three problems emerged:

1. **No designer role.** Layout and page template design requires `LayoutManage` and
   `ComponentManage` permissions. The only role that had them was `Editor`, which also
   grants content creation and publishing — undesirable separation of concerns.

2. **No system-level isolation.** There was no distinction between a tenant-scoped
   administrator and a system-wide administrator who can manage tenants.

3. **Approver ≠ Publisher.** The `Approver` role could review entries but not publish
   them, even though in most editorial workflows the approver is the one who publishes.

---

## Decision

Replace the five-role model with a **six-level hierarchy**. Legacy roles are preserved as
aliases pointing to the same permission sets, ensuring backwards compatibility.

### New hierarchy

```
SystemAdmin
└── SiteAdmin
    └── ContentAdmin
 ├── Designer
        ├── ContentApprover
        └── ContentAuthor
```

### Permission matrix

| Policy | SystemAdmin | SiteAdmin | ContentAdmin | Designer | ContentApprover | ContentAuthor |
|--------|:-----------:|:---------:|:------------:|:--------:|:---------------:|:-------------:|
| Entry.Read | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| Entry.Create | ✅ | ✅ | ✅ | — | — | ✅ |
| Entry.Update | ✅ | ✅ | ✅ | — | — | ✅ |
| Entry.Delete | ✅ | ✅ | ✅ | — | — | — |
| Entry.Publish | ✅ | ✅ | ✅ | — | ✅ | — |
| Entry.Review | ✅ | ✅ | ✅ | — | ✅ | — |
| Entry.Schedule | ✅ | ✅ | ✅ | — | — | — |
| ContentType.Manage | ✅ | ✅ | ✅ | — | — | — |
| Component.Manage | ✅ | ✅ | ✅ | ✅ | — | — |
| Layout.Manage | ✅ | ✅ | — | ✅ | — | — |
| Tenant.Manage | ✅ | ✅ | — | — | — | — |
| System.Admin | ✅ | — | — | — | — | — |

### Key design points

- **Additive model**: a user with multiple roles holds the union of all their permissions.
- **Designer isolates layout work**: `Designer` can manage layouts and components, but
  cannot create or publish content. This supports dedicated "front-end developer" accounts.
- **ContentApprover can publish**: aligns with the real-world workflow where reviewers
  are the ones who approve-and-publish.
- **ContentAuthor cannot delete**: prevents accidental data loss by junior authors.

### Legacy aliases

`TenantAdmin` → same permissions as `SiteAdmin`  
`Editor` → same permissions as `ContentAdmin` + `LayoutManage`  
`Approver` → same permissions as `ContentApprover` minus `Entry.Publish`  
`Author` → same permissions as `ContentAuthor`  
`Viewer` → read-only across all resource types  

---

## Consequences

**Positive:**
- Clear separation of editorial vs. design vs. administrative responsibilities.
- Designer accounts can be issued to external agencies without granting content access.
- Legacy JWTs with old role names continue to work unchanged.

**Negative:**
- Six roles increase onboarding documentation burden.
- `SiteAdmin` and `ContentAdmin` are similar; teams may default to `SiteAdmin` for
  everyone, defeating the purpose.

---

## Alternatives Considered

| Alternative | Rejected because |
|---|---|
| Attribute-based access control (ABAC) | Significant complexity increase; role-based is sufficient for the current scope |
| Permission bit-mask stored per user | Hard to manage in the admin UI; named roles are self-documenting |
| Keep 5 roles, add `Designer` only | Would leave the `SystemAdmin`/`SiteAdmin` distinction unresolved |
