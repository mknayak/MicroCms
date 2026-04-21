using MicroCMS.Shared.Ids;

namespace MicroCMS.Domain.Events.Content;

public sealed record ContentTypeCreatedEvent(
    ContentTypeId ContentTypeId,
    TenantId TenantId,
    string Handle) : DomainEvent;
