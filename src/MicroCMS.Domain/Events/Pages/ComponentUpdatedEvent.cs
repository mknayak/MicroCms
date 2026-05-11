using MicroCMS.Domain.Events;
using MicroCMS.Shared.Ids;

namespace MicroCMS.Domain.Events.Pages;

/// <summary>
/// Raised when a Component's template or schema changes.
/// Delivery must invalidate all rendered pages for the affected site because any page
/// rendering this component may have stale HTML.
/// </summary>
[OutboxDispatch(OutboxDispatchMode.Broadcast)]
public sealed record ComponentUpdatedEvent(
    ComponentId ComponentId,
    TenantId TenantId,
    SiteId SiteId) : DomainEvent;
