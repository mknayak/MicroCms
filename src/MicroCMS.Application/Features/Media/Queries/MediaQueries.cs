using MicroCMS.Application.Common.Attributes;
using MicroCMS.Application.Common.Authorization;
using MicroCMS.Application.Common.Markers;
using MicroCMS.Application.Features.Media.Dtos;
using MicroCMS.Shared.Primitives;

namespace MicroCMS.Application.Features.Media.Queries;

[HasPolicy(ContentPolicies.MediaRead)]
public sealed record GetMediaAssetQuery(Guid AssetId) : IQuery<MediaAssetDto>;

[HasPolicy(ContentPolicies.MediaRead)]
public sealed record ListMediaAssetsQuery(
    Guid SiteId,
    int Page = 1,
  int PageSize = 20) : IQuery<PagedList<MediaAssetListItemDto>>;
