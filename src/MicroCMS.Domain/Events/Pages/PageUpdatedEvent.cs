using MicroCMS.Shared.Ids;

namespace MicroCMS.Domain.Events.Pages;

/// <summary>
/// Raised when a page's content (title, slug, layout, site-template, SEO, placements, linked entry)
/// changes in Authoring. Delivery uses this to invalidate the rendered-page cache for the affected slug.
/// </summary>
public sealed record PageUpdatedEvent(
    PageId PageId,
    TenantId TenantId,
    SiteId SiteId,
    string Slug) : DomainEvent;
