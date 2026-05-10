namespace MicroCMS.Application.Features.Delivery.Caching;

/// <summary>
/// Centralises all cache key and tag patterns for the Delivery rendered-page cache.
/// Keeping keys in one place prevents typos and makes invalidation auditable.
/// </summary>
public static class DeliveryCacheKeys
{
    /// <summary>
    /// Per-page rendered-HTML cache key.
    /// Delivery populates this; Authoring worker invalidates it on page/entry change.
    /// </summary>
    public static string RenderedPage(Guid siteId, string slug)
        => $"delivery:page:{siteId}:{slug.ToLowerInvariant()}";

    /// <summary>
    /// Tag covering every rendered page for a site.
    /// Used for bulk invalidation when a layout, component, or site-template changes.
    /// </summary>
    public static string SiteTag(Guid siteId)
        => $"delivery:site:{siteId}";
}
