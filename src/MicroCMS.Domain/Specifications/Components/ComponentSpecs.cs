using MicroCMS.Domain.Aggregates.Components;
using MicroCMS.Domain.Specifications;
using MicroCMS.Shared.Ids;

namespace MicroCMS.Domain.Specifications.Components;

/// <summary>All components for a site, ordered by name. Paged.</summary>
public sealed class ComponentsBySiteSpec : BaseSpecification<Component>
{
    public ComponentsBySiteSpec(SiteId siteId, int page, int pageSize)
    : base(c => c.SiteId == siteId)
    {
        ApplyOrderBy(c => c.Name);
        ApplyPaging((page - 1) * pageSize, pageSize);
    }
}

/// <summary>Count-only version for paging metadata.</summary>
public sealed class ComponentsBySiteCountSpec : BaseSpecification<Component>
{
    public ComponentsBySiteCountSpec(SiteId siteId) : base(c => c.SiteId == siteId) { }
}

/// <summary>All components for a site (no paging) — used by package manager export.</summary>
public sealed class AllComponentsBySiteSpec : BaseSpecification<Component>
{
    public AllComponentsBySiteSpec(SiteId siteId) : base(c => c.SiteId == siteId)
    {
        ApplyOrderBy(c => c.Name);
    }
}
