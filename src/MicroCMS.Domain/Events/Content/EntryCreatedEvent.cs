using MicroCMS.Shared.Ids;

namespace MicroCMS.Domain.Events.Content;

public sealed record EntryCreatedEvent(
    EntryId EntryId,
    TenantId TenantId,
    SiteId SiteId,
    ContentTypeId ContentTypeId,
    string Slug,
    string Locale) : DomainEvent;
