using MicroCMS.Domain.Aggregates.Pages;
using MicroCMS.Domain.Specifications;
using MicroCMS.Domain.ValueObjects;
using MicroCMS.Shared.Ids;

namespace MicroCMS.Domain.Specifications.Delivery;

/// <summary>All pages for a site ordered by depth then title.</summary>
public sealed class PagesBySiteSpec : BaseSpecification<Page>
{
    public PagesBySiteSpec(SiteId siteId)
        : base(p => p.SiteId == siteId)
    {
        ApplyOrderBy(p => p.Depth);
    }
}

/// <summary>A single page by its slug value within a site.</summary>
public sealed class PageBySlugSpec : BaseSpecification<Page>
{
    public PageBySlugSpec(SiteId siteId, string slug)
        : this(siteId, Slug.Create(slug)) { }

  private PageBySlugSpec(SiteId siteId, Slug slug)
        : base(p => p.SiteId == siteId && p.Slug == slug) { }
}
