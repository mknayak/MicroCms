using MicroCMS.Domain.Aggregates.Content;
using MicroCMS.Domain.Specifications;
using MicroCMS.Shared.Ids;

namespace MicroCMS.Domain.Specifications.Content;

/// <summary>Returns all folders belonging to a site (GAP-02).</summary>
public sealed class FoldersBySiteSpec : BaseSpecification<Folder>
{
    public FoldersBySiteSpec(SiteId siteId)
    : base(f => f.SiteId == siteId)
    {
        ApplyOrderBy(f => f.Name);
    }
}
