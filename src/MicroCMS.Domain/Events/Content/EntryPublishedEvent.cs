using MicroCMS.Domain.Events;
using MicroCMS.Shared.Ids;

namespace MicroCMS.Domain.Events.Content;

[OutboxDispatch(OutboxDispatchMode.Broadcast)]
public sealed record EntryPublishedEvent(
    EntryId EntryId,
    TenantId TenantId,
    SiteId SiteId,
    string Slug,
    string Locale) : DomainEvent;
