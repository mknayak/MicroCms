using MicroCMS.Shared.Ids;

namespace MicroCMS.Domain.Events.Content;

public sealed record EntryPublishedEvent(
    EntryId EntryId,
    TenantId TenantId,
    SiteId SiteId,
    string Slug,
    string Locale) : DomainEvent;
