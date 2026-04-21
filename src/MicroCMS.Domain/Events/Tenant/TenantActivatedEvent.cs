using MicroCMS.Shared.Ids;

namespace MicroCMS.Domain.Events.Tenant;

public sealed record TenantActivatedEvent(TenantId TenantId) : DomainEvent;
