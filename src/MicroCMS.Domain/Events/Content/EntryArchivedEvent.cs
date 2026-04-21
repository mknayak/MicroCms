using MicroCMS.Shared.Ids;

namespace MicroCMS.Domain.Events.Content;

public sealed record EntryArchivedEvent(
    EntryId EntryId,
    TenantId TenantId) : DomainEvent;
