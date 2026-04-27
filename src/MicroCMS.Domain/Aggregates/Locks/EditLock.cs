using MicroCMS.Domain.Exceptions;
using MicroCMS.Shared.Ids;

namespace MicroCMS.Domain.Aggregates.Locks;

/// <summary>
/// Represents a pessimistic edit lock on a single entity.
/// </summary>
public sealed class EditLock : AggregateRoot<EditLockId>
{
    public const int TtlMinutes = 30;

    private EditLock() : base() { }

    private EditLock(EditLockId id, string entityId, string entityType,
     Guid lockedByUserId, string lockedByDisplayName) : base(id)
    {
     EntityId = entityId;
   EntityType = entityType;
     LockedByUserId = lockedByUserId;
        LockedByDisplayName = lockedByDisplayName;
    LockedAt = DateTimeOffset.UtcNow;
        ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(TtlMinutes);
    }

    public string EntityId { get; private set; } = string.Empty;
    public string EntityType { get; private set; } = string.Empty;
 public Guid LockedByUserId { get; private set; }
    public string LockedByDisplayName { get; private set; } = string.Empty;
    public DateTimeOffset LockedAt { get; private set; }
    public DateTimeOffset ExpiresAt { get; private set; }
    public bool IsExpired => DateTimeOffset.UtcNow >= ExpiresAt;

    public static EditLock Acquire(string entityId, string entityType, Guid userId, string displayName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(entityId, nameof(entityId));
        ArgumentException.ThrowIfNullOrWhiteSpace(entityType, nameof(entityType));
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName, nameof(displayName));
 return new EditLock(EditLockId.New(), entityId, entityType, userId, displayName);
    }

    public void Refresh()
    {
   if (IsExpired)
   throw new BusinessRuleViolationException("EditLock.Expired", "Cannot refresh an expired lock.");
        ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(TtlMinutes);
    }

    public bool IsOwnedBy(Guid userId) => LockedByUserId == userId;
}
