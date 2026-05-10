using MicroCMS.Shared.Ids;

namespace MicroCMS.Domain.Events.Pages;

/// <summary>
/// Raised when a SiteTemplate's placements or layout assignment changes.
/// Delivery must invalidate all rendered pages for the affected site because any page
/// using this site-template may have stale HTML.
/// </summary>
public sealed record SiteTemplateUpdatedEvent(
    SiteTemplateId SiteTemplateId,
    TenantId TenantId,
    SiteId SiteId) : DomainEvent;
