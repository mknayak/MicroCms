using MicroCMS.Application.Common.Attributes;
using MicroCMS.Application.Common.Authorization;
using MicroCMS.Application.Common.Markers;
using MicroCMS.Application.Features.Layouts.Dtos;

namespace MicroCMS.Application.Features.Layouts.Queries;

[HasPolicy(ContentPolicies.ComponentRead)]
public sealed record GetLayoutQuery(Guid LayoutId) : IQuery<LayoutDto>;

[HasPolicy(ContentPolicies.ComponentRead)]
public sealed record ListLayoutsQuery(Guid SiteId) : IQuery<IReadOnlyList<LayoutListItemDto>>;
