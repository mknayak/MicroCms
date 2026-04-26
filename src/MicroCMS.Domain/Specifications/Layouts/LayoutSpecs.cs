using MicroCMS.Domain.Aggregates.Components;
using MicroCMS.Domain.Specifications;
using MicroCMS.Shared.Ids;

namespace MicroCMS.Domain.Specifications.Layouts;

/// <summary>All layouts for a site ordered by name.</summary>
public sealed class LayoutsBySiteSpec : BaseSpecification<Layout>
{
    public LayoutsBySiteSpec(SiteId siteId)
        : base(l => l.SiteId == siteId)
    {
        ApplyOrderBy(l => l.Name);
    }
}

/// <summary>The default layout for a site.</summary>
public sealed class DefaultLayoutBySiteSpec : BaseSpecification<Layout>
{
    public DefaultLayoutBySiteSpec(SiteId siteId)
        : base(l => l.SiteId == siteId && l.IsDefault) { }
}
