using MicroCMS.Shared.Ids;

namespace MicroCMS.Domain.Events.Tenant;

public sealed record TenantCreatedEvent(
    TenantId TenantId,
    string Slug) : DomainEvent;
