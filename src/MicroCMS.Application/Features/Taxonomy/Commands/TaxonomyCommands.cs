using MicroCMS.Application.Common.Attributes;
using MicroCMS.Application.Common.Authorization;
using MicroCMS.Application.Common.Markers;
using MicroCMS.Application.Features.Taxonomy.Dtos;

namespace MicroCMS.Application.Features.Taxonomy.Commands;

[HasPolicy(ContentPolicies.TaxonomyManage)]
public sealed record CreateCategoryCommand(
    Guid SiteId,
    string Name,
    string Slug,
    Guid? ParentId = null,
    string? Description = null) : ICommand<CategoryDto>;

[HasPolicy(ContentPolicies.TaxonomyManage)]
public sealed record DeleteCategoryCommand(Guid CategoryId) : ICommand;

[HasPolicy(ContentPolicies.TaxonomyManage)]
public sealed record CreateTagCommand(
    Guid SiteId,
    string Name,
 string Slug) : ICommand<TagDto>;

[HasPolicy(ContentPolicies.TaxonomyManage)]
public sealed record DeleteTagCommand(Guid TagId) : ICommand;
