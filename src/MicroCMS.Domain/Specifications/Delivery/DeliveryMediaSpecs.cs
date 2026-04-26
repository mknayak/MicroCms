using MicroCMS.Domain.Aggregates.Media;
using MicroCMS.Domain.Enums;
using MicroCMS.Domain.Specifications;
using MicroCMS.Shared.Ids;

namespace MicroCMS.Domain.Specifications.Delivery;

/// <summary>
/// Available (non-deleted, non-quarantined) media assets for a site.
/// Used by the Delivery API to list publicly accessible assets.
/// </summary>
public sealed class AvailableMediaAssetsBySiteSpec : BaseSpecification<MediaAsset>
{
    public AvailableMediaAssetsBySiteSpec(SiteId siteId, int page, int pageSize)
        : base(a => a.SiteId == siteId && a.Status == MediaAssetStatus.Available)
    {
        ApplyOrderByDescending(a => a.CreatedAt);
        ApplyPaging((page - 1) * pageSize, pageSize);
    }

    /// <summary>Count-only overload (no paging).</summary>
    public AvailableMediaAssetsBySiteSpec(SiteId siteId)
        : base(a => a.SiteId == siteId && a.Status == MediaAssetStatus.Available)
    {
    }
}

/// <summary>A single available media asset by ID — enforces status check at query time.</summary>
public sealed class AvailableMediaAssetByIdSpec : BaseSpecification<MediaAsset>
{
    public AvailableMediaAssetByIdSpec(MediaAssetId id)
        : base(a => a.Id == id && a.Status == MediaAssetStatus.Available)
{
    }
}
