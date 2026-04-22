using FluentValidation.Results;
using MediatR;
using MicroCMS.Application.Common.Exceptions;
using MicroCMS.Application.Features.ContentTypes.Dtos;
using MicroCMS.Application.Features.ContentTypes.Mappers;
using MicroCMS.Application.Features.ContentTypes.Queries;
using MicroCMS.Domain.Aggregates.Content;
using MicroCMS.Domain.Repositories;
using MicroCMS.Domain.Specifications.Content;
using MicroCMS.Shared.Ids;
using MicroCMS.Shared.Primitives;
using MicroCMS.Shared.Results;

namespace MicroCMS.Application.Features.ContentTypes.Handlers;

internal sealed class GetContentTypeQueryHandler(
    IRepository<ContentType, ContentTypeId> repo)
    : IRequestHandler<GetContentTypeQuery, Result<ContentTypeDto>>
{
    public async Task<Result<ContentTypeDto>> Handle(GetContentTypeQuery request, CancellationToken cancellationToken)
    {
    var ct = await repo.GetByIdAsync(new ContentTypeId(request.ContentTypeId), cancellationToken)
     ?? throw new NotFoundException(nameof(ContentType), request.ContentTypeId);
        return Result.Success(ContentTypeMapper.ToDto(ct));
    }
}

internal sealed class ListContentTypesQueryHandler(
    IRepository<ContentType, ContentTypeId> repo)
    : IRequestHandler<ListContentTypesQuery, Result<PagedList<ContentTypeListItemDto>>>
{
 public async Task<Result<PagedList<ContentTypeListItemDto>>> Handle(ListContentTypesQuery request, CancellationToken cancellationToken)
    {
   var siteId = new SiteId(request.SiteId);
        var spec = new ContentTypesBySitePagedSpec(siteId, request.Page, request.PageSize);
    var countSpec = new ContentTypesBySiteSpec(siteId);

       var items = await repo.ListAsync(spec, cancellationToken);
        var total = await repo.CountAsync(countSpec, cancellationToken);

        return Result.Success(PagedList<ContentTypeListItemDto>.Create(
      items.Select(ContentTypeMapper.ToListItemDto),
 request.Page, request.PageSize, total));
    }
}
