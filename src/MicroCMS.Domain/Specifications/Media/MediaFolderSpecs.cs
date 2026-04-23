using MicroCMS.Domain.Aggregates.Media;
using MicroCMS.Domain.Specifications;
using MicroCMS.Shared.Ids;

namespace MicroCMS.Domain.Specifications.Media;

/// <summary>All media folders for a site, optionally scoped to a parent folder.</summary>
public sealed class MediaFoldersBySiteSpec : BaseSpecification<MediaFolder>
{
    public MediaFoldersBySiteSpec(SiteId siteId, Guid? parentFolderId)
        : base(f => f.SiteId == siteId && f.ParentFolderId == parentFolderId)
    {
        ApplyOrderBy(f => f.Name);
    }
}

/// <summary>All direct child folders of a given parent (used to block non-empty folder deletion).</summary>
public sealed class ChildMediaFoldersSpec : BaseSpecification<MediaFolder>
{
    public ChildMediaFoldersSpec(Guid parentFolderId)
        : base(f => f.ParentFolderId == parentFolderId) { }
}

/// <summary>All assets that live directly in the specified folder (used to block non-empty folder deletion).</summary>
public sealed class MediaAssetsByFolderSpec : BaseSpecification<MediaAsset>
{
    public MediaAssetsByFolderSpec(Guid folderId)
        : base(a => a.FolderId == folderId
                 && a.Status != MicroCMS.Domain.Enums.MediaAssetStatus.Deleted) { }
}

/// <summary>Returns a page of PendingScan assets for the background scan job.</summary>
public sealed class PendingScanAssetsSpec : BaseSpecification<MediaAsset>
{
    public PendingScanAssetsSpec(int batchSize)
        : base(a => a.Status == MicroCMS.Domain.Enums.MediaAssetStatus.PendingScan)
    {
        ApplyOrderBy(a => a.CreatedAt);
        ApplyPaging(0, batchSize);
    }
}
