using MicroCMS.Domain.Aggregates.Media;
using MicroCMS.Domain.Enums;
using MicroCMS.Shared.Ids;

namespace MicroCMS.Domain.Specifications.Media;

/// <summary>Returns available media assets within a specific folder (or root if folderId is null).</summary>
public sealed class AvailableAssetsByFolderSpec : BaseSpecification<MediaAsset>
{
    public AvailableAssetsByFolderSpec(TenantId tenantId, SiteId siteId, Guid? folderId, int page, int pageSize)
        : base(a => a.TenantId == tenantId
                 && a.SiteId == siteId
                 && a.FolderId == folderId
                 && a.Status == MediaAssetStatus.Available)
    {
        ApplyOrderByDescending(a => a.CreatedAt);
        ApplyPaging((page - 1) * pageSize, pageSize);
    }
}
