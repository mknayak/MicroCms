using MicroCMS.Shared.Ids;

namespace MicroCMS.Domain.Events.Identity;

/// <summary>Raised after a successful password-based login.</summary>
public sealed record UserLoggedInEvent(
    UserId UserId,
    TenantId TenantId,
    string Email,
    string? IpAddress) : DomainEvent;
