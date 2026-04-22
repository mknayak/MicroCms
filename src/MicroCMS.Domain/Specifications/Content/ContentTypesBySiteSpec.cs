using MicroCMS.Domain.Aggregates.Content;
using MicroCMS.Domain.Specifications;
using MicroCMS.Shared.Ids;

namespace MicroCMS.Domain.Specifications.Content;

/// <summary>Returns all content types for a given site, ordered by handle.</summary>
public sealed class ContentTypesBySiteSpec : BaseSpecification<ContentType>
{
    public ContentTypesBySiteSpec(SiteId siteId)
   : base(ct => ct.SiteId == siteId)
    {
        ApplyOrderBy(ct => ct.Handle);
    }
}

/// <summary>Paged overload — applies paging on top of the site filter.</summary>
public sealed class ContentTypesBySitePagedSpec : BaseSpecification<ContentType>
{
    public ContentTypesBySitePagedSpec(SiteId siteId, int page, int pageSize)
        : base(ct => ct.SiteId == siteId)
    {
        ApplyOrderBy(ct => ct.Handle);
        ApplyPaging((page - 1) * pageSize, pageSize);
    }
}
