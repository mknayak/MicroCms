using MicroCMS.Domain.Aggregates.Content;
using MicroCMS.Domain.Enums;
using MicroCMS.Shared.Ids;

namespace MicroCMS.Domain.Specifications.Content;

/// <summary>Returns paginated published entries for a tenant+site, newest first.</summary>
public sealed class EntriesByTenantSpec : BaseSpecification<Entry>
{
    public EntriesByTenantSpec(TenantId tenantId, SiteId siteId, int page, int pageSize)
        : base(e => e.TenantId == tenantId
                 && e.SiteId == siteId
                 && e.Status == EntryStatus.Published)
    {
        ApplyOrderByDescending(e => e.PublishedAt!);
        ApplyPaging((page - 1) * pageSize, pageSize);
    }
}
