using MicroCMS.Shared.Ids;

namespace MicroCMS.Domain.Events.Content;

public sealed record EntryUpdatedEvent(
    EntryId EntryId,
    TenantId TenantId,
    Guid EditorId) : DomainEvent;
