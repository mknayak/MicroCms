using MicroCMS.Domain.Aggregates.Content;
using MicroCMS.Domain.Specifications;
using MicroCMS.Shared.Ids;

namespace MicroCMS.Domain.Specifications.Content;

/// <summary>
/// Finds the <see cref="ContentType"/> that matches a specific <paramref name="handle"/>
/// within a given site.  Used by the page-creation handler to locate the built-in
/// <c>page</c> content type so a linked entry can be created automatically.
/// </summary>
public sealed class ContentTypeByHandleAndSiteSpec : BaseSpecification<ContentType>
{
    public ContentTypeByHandleAndSiteSpec(SiteId siteId, string handle)
        : base(ct => ct.SiteId == siteId && ct.Handle == handle)
    {
    }
}
