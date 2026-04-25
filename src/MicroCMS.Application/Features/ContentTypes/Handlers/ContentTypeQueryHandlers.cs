using FluentValidation.Results;
using MediatR;
using MicroCMS.Application.Common.Exceptions;
using MicroCMS.Application.Common.Interfaces;
using MicroCMS.Application.Features.ContentTypes.Dtos;
using MicroCMS.Application.Features.ContentTypes.Mappers;
using MicroCMS.Application.Features.ContentTypes.Queries;
using MicroCMS.Application.Features.Search.EventHandlers;
using MicroCMS.Domain.Aggregates.Content;
using MicroCMS.Domain.Repositories;
using MicroCMS.Domain.Specifications;
using MicroCMS.Domain.Specifications.Content;
using MicroCMS.Shared.Ids;
using MicroCMS.Shared.Primitives;
using MicroCMS.Shared.Results;

namespace MicroCMS.Application.Features.ContentTypes.Handlers;

internal sealed class GetContentTypeQueryHandler(
    IRepository<ContentType, ContentTypeId> repo,
    ICacheService cacheService,
    ICurrentUser currentUser)
    : IRequestHandler<GetContentTypeQuery, Result<ContentTypeDto>>
{
    public async Task<Result<ContentTypeDto>> Handle(GetContentTypeQuery request, CancellationToken cancellationToken)
    {
        var tenantId = currentUser.TenantId;
        var cacheKey = CacheKeys.ContentType(tenantId, request.ContentTypeId);

     var cached = await cacheService.GetAsync<ContentTypeDto>(cacheKey, cancellationToken);
     if (cached is not null)
          return Result.Success(cached);

        var ct = await repo.GetByIdAsync(new ContentTypeId(request.ContentTypeId), cancellationToken)
     ?? throw new NotFoundException(nameof(ContentType), request.ContentTypeId);

      var dto = ContentTypeMapper.ToDto(ct);
      await cacheService.SetWithTagAsync(cacheKey, dto, CacheTags.TenantContentTypes(tenantId), cancellationToken: cancellationToken);

        return Result.Success(dto);
    }
}

internal sealed class ListContentTypesQueryHandler(
    IRepository<ContentType, ContentTypeId> repo,
    ICacheService cacheService,
    ICurrentUser currentUser)
  : IRequestHandler<ListContentTypesQuery, Result<PagedList<ContentTypeListItemDto>>>
{
    public async Task<Result<PagedList<ContentTypeListItemDto>>> Handle(ListContentTypesQuery request, CancellationToken cancellationToken)
{
     var tenantId = currentUser.TenantId;
        var cacheKey = CacheKeys.ContentTypeList(tenantId, request.SiteId, request.Page, request.PageSize);

  var cached = await cacheService.GetAsync<PagedList<ContentTypeListItemDto>>(cacheKey, cancellationToken);
        if (cached is not null)
   return Result.Success(cached);

        ISpecification<ContentType> spec;
      ISpecification<ContentType> countSpec;

        if (request.SiteId.HasValue)
    {
    var siteId = new SiteId(request.SiteId.Value);
 spec = new ContentTypesBySitePagedSpec(siteId, request.Page, request.PageSize);
            countSpec = new ContentTypesBySiteSpec(siteId);
        }
        else
{
      spec = new AllContentTypesPagedSpec(request.Page, request.PageSize);
   countSpec = new AllContentTypesCountSpec();
        }

        var items = await repo.ListAsync(spec, cancellationToken);
     var total = await repo.CountAsync(countSpec, cancellationToken);

   var paged = PagedList<ContentTypeListItemDto>.Create(
    items.Select(ContentTypeMapper.ToListItemDto),
            request.Page, request.PageSize, total);

      await cacheService.SetWithTagAsync(cacheKey, paged, CacheTags.TenantContentTypes(tenantId), cancellationToken: cancellationToken);

        return Result.Success(paged);
    }
}
