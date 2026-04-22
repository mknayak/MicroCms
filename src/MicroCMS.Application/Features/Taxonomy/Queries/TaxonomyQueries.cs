using MicroCMS.Application.Common.Attributes;
using MicroCMS.Application.Common.Authorization;
using MicroCMS.Application.Common.Markers;
using MicroCMS.Application.Features.Taxonomy.Dtos;

namespace MicroCMS.Application.Features.Taxonomy.Queries;

[HasPolicy(ContentPolicies.TaxonomyRead)]
public sealed record ListCategoriesQuery(Guid SiteId) : IQuery<IReadOnlyList<CategoryDto>>;

[HasPolicy(ContentPolicies.TaxonomyRead)]
public sealed record ListTagsQuery(Guid SiteId) : IQuery<IReadOnlyList<TagDto>>;
