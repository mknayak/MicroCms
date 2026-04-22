using MicroCMS.Domain.Aggregates.Media;
using MicroCMS.Domain.Specifications;
using MicroCMS.Shared.Ids;

namespace MicroCMS.Domain.Specifications.Media;

/// <summary>Returns all media assets for a site, newest first.</summary>
public sealed class MediaAssetsBySiteSpec : BaseSpecification<MediaAsset>
{
    public MediaAssetsBySiteSpec(SiteId siteId)
        : base(a => a.SiteId == siteId)
    {
ApplyOrderByDescending(a => a.CreatedAt);
    }
}

/// <summary>Paged overload.</summary>
public sealed class MediaAssetsBySitePagedSpec : BaseSpecification<MediaAsset>
{
 public MediaAssetsBySitePagedSpec(SiteId siteId, int page, int pageSize)
        : base(a => a.SiteId == siteId)
    {
        ApplyOrderByDescending(a => a.CreatedAt);
        ApplyPaging((page - 1) * pageSize, pageSize);
    }
}
