using MicroCMS.Domain.Aggregates.Content;
using MicroCMS.Shared.Ids;

namespace MicroCMS.Domain.Specifications.Content;

/// <summary>
/// Returns entries for a site, with optional status filter.
/// The paging constructor is used by ListEntries query for data retrieval.
/// The non-paging constructor is used by count-only queries.
/// </summary>
public sealed class EntriesBySiteSpec : BaseSpecification<Entry>
{
    /// <summary>Paginated constructor for data retrieval.</summary>
    public EntriesBySiteSpec(SiteId siteId, string? statusFilter, int page, int pageSize)
        : base(e => e.SiteId == siteId
                 && (statusFilter == null || e.Status.ToString() == statusFilter))
    {
        ApplyOrderByDescending(e => e.UpdatedAt);
        ApplyPaging((page - 1) * pageSize, pageSize);
    }

    /// <summary>Non-paged constructor for count-only queries.</summary>
    public EntriesBySiteSpec(SiteId siteId, string? statusFilter)
        : base(e => e.SiteId == siteId
                 && (statusFilter == null || e.Status.ToString() == statusFilter))
    {
    }
}
