using MicroCMS.Domain.Aggregates.Components;
using MicroCMS.Domain.Specifications;
using MicroCMS.Shared.Ids;

namespace MicroCMS.Domain.Specifications.Layouts;

/// <summary>The PageTemplate for a specific page.</summary>
public sealed class PageTemplateByPageSpec : BaseSpecification<PageTemplate>
{
  public PageTemplateByPageSpec(PageId pageId)
        : base(t => t.PageId == pageId) { }
}
