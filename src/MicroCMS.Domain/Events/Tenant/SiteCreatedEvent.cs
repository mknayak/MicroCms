using MicroCMS.Shared.Ids;

namespace MicroCMS.Domain.Events.Tenant;

public sealed record SiteCreatedEvent(
    TenantId TenantId,
    SiteId SiteId,
    string Handle) : DomainEvent;
