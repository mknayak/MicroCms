using MicroCMS.Shared.Ids;

namespace MicroCMS.Domain.Events.Pages;

/// <summary>
/// Raised when a Layout shell, zones, config, or default placements change.
/// Delivery must invalidate all rendered pages for the affected site because any page
/// using this layout may have stale HTML.
/// </summary>
public sealed record LayoutUpdatedEvent(
    LayoutId LayoutId,
    TenantId TenantId,
    SiteId SiteId) : DomainEvent;
