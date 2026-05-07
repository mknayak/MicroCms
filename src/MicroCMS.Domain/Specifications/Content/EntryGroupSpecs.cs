using MicroCMS.Domain.Aggregates.Content;
using MicroCMS.Shared.Ids;

namespace MicroCMS.Domain.Specifications.Content;

/// <summary>All groups belonging to a site (optionally filtered by content type).</summary>
public sealed class EntryGroupsBySiteSpec : BaseSpecification<EntryGroup>
{
    public EntryGroupsBySiteSpec(SiteId siteId, Guid? contentTypeId = null)
        : base(g => g.SiteId == siteId
                 && (contentTypeId == null || g.ContentTypeId == new ContentTypeId(contentTypeId.Value)))
    {
        AddInclude(g => g.Members);
        ApplyOrderBy(g => g.Title);
    }
}

/// <summary>Finds a single group by handle within a site + content type.</summary>
public sealed class EntryGroupByHandleSpec : BaseSpecification<EntryGroup>
{
    public EntryGroupByHandleSpec(SiteId siteId, ContentTypeId contentTypeId, string handle)
        : base(g => g.SiteId == siteId
                 && g.ContentTypeId == contentTypeId
                 && g.Handle == handle)
    {
        AddInclude(g => g.Members);
    }
}
