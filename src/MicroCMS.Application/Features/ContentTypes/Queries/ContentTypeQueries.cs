using MicroCMS.Application.Common.Attributes;
using MicroCMS.Application.Common.Authorization;
using MicroCMS.Application.Common.Markers;
using MicroCMS.Application.Features.ContentTypes.Dtos;
using MicroCMS.Shared.Primitives;

namespace MicroCMS.Application.Features.ContentTypes.Queries;

[HasPolicy(ContentPolicies.ContentTypeRead)]
public sealed record GetContentTypeQuery(Guid ContentTypeId) : IQuery<ContentTypeDto>;

[HasPolicy(ContentPolicies.ContentTypeRead)]
public sealed record ListContentTypesQuery(
    Guid? SiteId,
    int Page = 1,
    int PageSize = 20) : IQuery<PagedList<ContentTypeListItemDto>>;
