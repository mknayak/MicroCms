using MicroCMS.Domain.Events;
using MicroCMS.Shared.Ids;

namespace MicroCMS.Domain.Events.Content;

[OutboxDispatch(OutboxDispatchMode.Broadcast)]
public sealed record EntryUnpublishedEvent(
    EntryId EntryId,
    TenantId TenantId) : DomainEvent;
