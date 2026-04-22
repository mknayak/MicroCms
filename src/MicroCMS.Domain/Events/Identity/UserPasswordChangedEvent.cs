using MicroCMS.Shared.Ids;

namespace MicroCMS.Domain.Events.Identity;

/// <summary>Raised after a user's password has been changed.</summary>
public sealed record UserPasswordChangedEvent(
    UserId UserId,
    TenantId TenantId) : DomainEvent;
