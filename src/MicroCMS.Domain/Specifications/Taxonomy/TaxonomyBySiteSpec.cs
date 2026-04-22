using MicroCMS.Domain.Aggregates.Taxonomy;
using MicroCMS.Domain.Specifications;
using MicroCMS.Shared.Ids;

namespace MicroCMS.Domain.Specifications.Taxonomy;

/// <summary>All categories for a site ordered by name.</summary>
public sealed class CategoriesBySiteSpec : BaseSpecification<Category>
{
    public CategoriesBySiteSpec(SiteId siteId)
        : base(c => c.SiteId == siteId)
    {
        ApplyOrderBy(c => c.Name);
    }
}

/// <summary>All tags for a site ordered by name.</summary>
public sealed class TagsBySiteSpec : BaseSpecification<Tag>
{
    public TagsBySiteSpec(SiteId siteId)
        : base(t => t.SiteId == siteId)
    {
        ApplyOrderBy(t => t.Name);
    }
}
