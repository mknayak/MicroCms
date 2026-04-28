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

/// <summary>All items for a specific component. Paged.</summary>
public sealed class ComponentItemsByComponentSpec : BaseSpecification<ComponentItem>
{
    public ComponentItemsByComponentSpec(ComponentId componentId, int page, int pageSize)
        : base(ci => ci.ComponentId == componentId)
    {
        ApplyOrderByDescending(ci => ci.UpdatedAt);
  ApplyPaging((page - 1) * pageSize, pageSize);
    }
}

/// <summary>Items for a component filtered by status. Paged.</summary>
public sealed class ComponentItemsByComponentAndStatusSpec : BaseSpecification<ComponentItem>
{
    public ComponentItemsByComponentAndStatusSpec(
        ComponentId componentId,
    ComponentItemStatus status,
        int page,
        int pageSize)
        : base(ci => ci.ComponentId == componentId && ci.Status == status)
  {
        ApplyOrderByDescending(ci => ci.UpdatedAt);
     ApplyPaging((page - 1) * pageSize, pageSize);
    }
}

/// <summary>Count all items for a component (optional status filter).</summary>
public sealed class ComponentItemsCountSpec : BaseSpecification<ComponentItem>
{
    public ComponentItemsCountSpec(ComponentId componentId)
   : base(ci => ci.ComponentId == componentId) { }
}

/// <summary>All components for a site (no paging) — used by package manager export.</summary>
public sealed class AllComponentsBySiteSpec : BaseSpecification<Component>
{
    public AllComponentsBySiteSpec(SiteId siteId) : base(c => c.SiteId == siteId)
  {
   ApplyOrderBy(c => c.Name);
    }
}
