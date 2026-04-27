using MicroCMS.Domain.Aggregates.Components;
using MicroCMS.Domain.Specifications;
using MicroCMS.Shared.Ids;

namespace MicroCMS.Domain.Specifications.SiteTemplates;

/// <summary>Returns all site templates belonging to a specific site, ordered by name.</summary>
public sealed class SiteTemplatesBySiteSpec : BaseSpecification<SiteTemplate>
{
    public SiteTemplatesBySiteSpec(SiteId siteId)
        : base(t => t.SiteId == siteId)
    {
  ApplyOrderBy(t => t.Name);
    }
}
