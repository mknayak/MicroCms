using MicroCMS.Shared.Ids;

namespace MicroCMS.Domain.Events.Tenant;

public sealed record TenantSuspendedEvent(
    TenantId TenantId,
    string Reason) : DomainEvent;
