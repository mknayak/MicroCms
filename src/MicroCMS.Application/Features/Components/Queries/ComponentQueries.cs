using MicroCMS.Application.Common.Attributes;
using MicroCMS.Application.Common.Authorization;
using MicroCMS.Application.Common.Markers;
using MicroCMS.Application.Features.Components.Dtos;
using MicroCMS.Shared.Primitives;

namespace MicroCMS.Application.Features.Components.Queries;

[HasPolicy(ContentPolicies.ComponentRead)]
public sealed record ListComponentsQuery(
    Guid SiteId,
    int Page = 1,
    int PageSize = 50) : IQuery<PagedList<ComponentListItemDto>>;

[HasPolicy(ContentPolicies.ComponentRead)]
public sealed record GetComponentQuery(Guid ComponentId) : IQuery<ComponentDto>;

[HasPolicy(ContentPolicies.ComponentRead)]
public sealed record ListComponentItemsQuery(
    Guid ComponentId,
    string? Status = null,
    int Page = 1,
    int PageSize = 50) : IQuery<PagedList<ComponentItemDto>>;

[HasPolicy(ContentPolicies.ComponentRead)]
public sealed record GetComponentItemQuery(Guid ComponentId, Guid ItemId) : IQuery<ComponentItemDto>;
