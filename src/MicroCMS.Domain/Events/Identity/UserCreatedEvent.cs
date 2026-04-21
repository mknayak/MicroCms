using MicroCMS.Domain.Enums;
using MicroCMS.Shared.Ids;

namespace MicroCMS.Domain.Events.Identity;

public sealed record UserCreatedEvent(
    UserId UserId,
    TenantId TenantId,
    string Email) : DomainEvent;
