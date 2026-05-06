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

/// <summary>Paged overload filtered by optional folder and/or search term.</summary>
public sealed class MediaAssetsBySiteFilteredSpec : BaseSpecification<MediaAsset>
{
    public MediaAssetsBySiteFilteredSpec(
        SiteId siteId,
        Guid? folderId,
        string? search,
        int page,
        int pageSize)
        : base(a => a.SiteId == siteId
                 && (folderId == null || a.FolderId == folderId)
                 && (search == null || a.Metadata.FileName.Contains(search)))
    {
        ApplyOrderByDescending(a => a.CreatedAt);
        ApplyPaging((page - 1) * pageSize, pageSize);
    }
}

/// <summary>Count overload matching the same folder/search filter.</summary>
public sealed class MediaAssetsBySiteFilteredCountSpec : BaseSpecification<MediaAsset>
{
    public MediaAssetsBySiteFilteredCountSpec(SiteId siteId, Guid? folderId, string? search)
        : base(a => a.SiteId == siteId
                 && (folderId == null || a.FolderId == folderId)
                 && (search == null || a.Metadata.FileName.Contains(search)))
    {
    }
}
