using MicroCMS.Shared.Ids;

namespace MicroCMS.Domain.Events.Content;

public sealed record EntryUnpublishedEvent(
    EntryId EntryId,
    TenantId TenantId) : DomainEvent;
