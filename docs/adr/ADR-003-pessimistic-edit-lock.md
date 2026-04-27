# ADR-003: Pessimistic Edit Lock

**Status:** Accepted  
**Date:** 2025-01  
**Deciders:** Engineering team  
**Files affected:**
- `src/MicroCMS.Domain/Aggregates/Locks/EditLock.cs`
- `src/MicroCMS.Shared/Ids/EditLockId.cs`
- `src/MicroCMS.Domain/Specifications/Locks/LockSpecs.cs`
- `src/MicroCMS.Infrastructure/Persistence/Common/Configurations/EditLockConfiguration.cs`
- `src/MicroCMS.Application/Features/Locks/Commands/LockCommands.cs`
- `src/MicroCMS.Application/Features/Locks/Handlers/LockHandlers.cs`
- `src/MicroCMS.Api/Controllers/LocksController.cs`
- `src/MicroCMS.Admin.WebHost/ClientApp/src/api/layouts.ts` (locksApi)

---

## Context

MicroCMS supports collaborative editorial teams. Without concurrency protection,
two editors can open the same entry or page template simultaneously, make independent
changes, and silently overwrite each other's work on save.

Two standard strategies exist:

- **Optimistic concurrency** (row version / ETag) — detects conflicts on save and asks
  the user to merge.
- **Pessimistic locking** — prevents the second user from editing while the first holds
  a lock.

The CMS editorial workflow prioritises **preventing** conflicts over resolving them,
because content (especially structured JSON fields) is hard to merge meaningfully in a UI.

---

## Decision

Implement **pessimistic edit locks** via a dedicated `EditLock` aggregate.

### Lock model

```
EntityId   : string  — opaque identifier of the locked entity
EntityType        : string  — "entry" | "page-template" | "layout"
LockedByUserId    : Guid    — raw Guid, matching ICurrentUser.UserId
LockedByDisplayName : string
LockedAt          : DateTimeOffset
ExpiresAt         : DateTimeOffset  — LockedAt + 30 minutes
```

### TTL: 30 minutes

A lock automatically expires after **30 minutes** of inactivity (`EditLock.TtlMinutes = 30`).
The editing client is expected to call `POST /locks/{entityId}/refresh` on a heartbeat
(e.g., every 5 minutes) to extend the lock while the editor is active.

### Acquire semantics

- If no lock exists, or the existing lock is expired → create a new lock.
- If a non-expired lock is held by the **same user** → replace it (renew).
- If a non-expired lock is held by a **different user** → `409 Conflict` with the holder's
  display name and expiry time in the error message.

### Release

- Only the lock owner or a `SystemAdmin` may release a lock.
- Release is **idempotent** — releasing a non-existent lock returns `204 No Content`.

### Expired lock cleanup

Expired locks are not deleted eagerly. They are overwritten on the next `Acquire` call.
A background cleanup job (future work) can purge stale records.

### LockedByUserId as Guid

`ICurrentUser.UserId` is `Guid`. The lock stores `Guid` directly rather than a `UserId`
value object, avoiding an unnecessary wrapper and keeping the aggregate simple.

### API

| Method | Path | Description |
|--------|------|-------------|
| `GET` | `/locks/{entityId}` | Returns current lock or `null` if unlocked/expired |
| `POST` | `/locks/acquire` | Acquire or renew a lock |
| `DELETE` | `/locks/{entityId}` | Release a lock |
| `POST` | `/locks/{entityId}/refresh` | Extend TTL — owner only |

---

## Consequences

**Positive:**
- No merge conflicts in the editorial UI.
- Simple implementation: one aggregate, one DB table, four endpoints.
- TTL prevents locks being held indefinitely when a browser tab is closed.

**Negative:**
- Does not protect against API-level concurrent writes (e.g., two automation scripts).
  Optimistic concurrency at the database level remains advisable as a second layer.
- An editor who loses network connectivity holds the lock until expiry (30 min).
- Adds a network round-trip (heartbeat) for editors with long session duration.

---

## Alternatives Considered

| Alternative | Rejected because |
|---|---|
| Optimistic concurrency (ETag) only | Hard to merge structured JSON field content in a UI |
| SignalR presence / real-time notification | Higher operational complexity; overkill for initial release |
| Redis-backed distributed lock | Adds infrastructure dependency; DB-backed is sufficient for single-region deployment |
| Lock TTL of 60 minutes | Too long for a typical page template editing session; 30 min is the industry norm (Contentful, Storyblok) |
