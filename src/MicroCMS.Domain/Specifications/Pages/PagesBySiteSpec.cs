using MicroCMS.Domain.Aggregates.Pages;
using MicroCMS.Domain.Specifications;
using MicroCMS.Shared.Ids;

namespace MicroCMS.Domain.Specifications;

/// <summary>Returns all pages belonging to a site ordered by depth then slug (GAP-21).</summary>
public sealed class PagesBySiteSpec : BaseSpecification<Page>
{
    public PagesBySiteSpec(SiteId siteId) : base(p => p.SiteId == siteId)
    {
        ApplyOrderBy(p => p.Depth);
    }
}
