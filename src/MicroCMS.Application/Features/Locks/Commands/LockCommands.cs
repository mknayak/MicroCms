using MicroCMS.Application.Common.Attributes;
using MicroCMS.Application.Common.Authorization;
using MicroCMS.Application.Common.Markers;

namespace MicroCMS.Application.Features.Locks.Commands;

public sealed record EditLockDto(
    string EntityId,
    string EntityType,
 Guid LockedByUserId,
    string LockedByDisplayName,
 DateTimeOffset LockedAt,
    DateTimeOffset ExpiresAt);

// Lock acquisition requires auth but no specific content policy — any authenticated user can lock
[HasPolicy(ContentPolicies.EntryUpdate)]
public sealed record AcquireLockCommand(
    string EntityId,
    string EntityType) : ICommand<EditLockDto>;

[HasPolicy(ContentPolicies.EntryUpdate)]
public sealed record ReleaseLockCommand(string EntityId) : ICommand;

[HasPolicy(ContentPolicies.EntryUpdate)]
public sealed record RefreshLockCommand(string EntityId) : ICommand<EditLockDto>;

public sealed record GetLockQuery(string EntityId) : IQuery<EditLockDto?>;
