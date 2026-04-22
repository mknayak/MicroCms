using MediatR;
using MicroCMS.Application.Common.Exceptions;
using MicroCMS.Application.Common.Interfaces;
using MicroCMS.Application.Features.Media.Commands;
using MicroCMS.Application.Features.Media.Dtos;
using MicroCMS.Application.Features.Media.Mappers;
using MicroCMS.Application.Features.Media.Queries;
using MicroCMS.Domain.Aggregates.Media;
using MicroCMS.Domain.Repositories;
using MicroCMS.Domain.Specifications.Media;
using MicroCMS.Domain.ValueObjects;
using MicroCMS.Shared.Ids;
using MicroCMS.Shared.Primitives;
using MicroCMS.Shared.Results;

namespace MicroCMS.Application.Features.Media.Handlers;

internal sealed class RegisterMediaAssetCommandHandler(
    IRepository<MediaAsset, MediaAssetId> repo,
    ICurrentUser currentUser)
    : IRequestHandler<RegisterMediaAssetCommand, Result<MediaAssetDto>>
{
    public async Task<Result<MediaAssetDto>> Handle(RegisterMediaAssetCommand request, CancellationToken cancellationToken)
    {
       var metadata = AssetMetadata.Create(
           request.FileName, request.MimeType, request.SizeBytes, request.WidthPx, request.HeightPx);

 var asset = MediaAsset.Create(
            currentUser.TenantId, new SiteId(request.SiteId),
          metadata, request.StorageKey, currentUser.UserId, request.FolderId);

      // Sprint 4: mark available immediately — real scan pipeline arrives in Sprint 7
   asset.MarkUploadComplete();
    asset.MarkAvailable();

        await repo.AddAsync(asset, cancellationToken);
        return Result.Success(MediaMapper.ToDto(asset));
    }
}

internal sealed class UpdateMediaAssetMetadataCommandHandler(
    IRepository<MediaAsset, MediaAssetId> repo)
    : IRequestHandler<UpdateMediaAssetMetadataCommand, Result<MediaAssetDto>>
{
    public async Task<Result<MediaAssetDto>> Handle(UpdateMediaAssetMetadataCommand request, CancellationToken cancellationToken)
    {
        var asset = await repo.GetByIdAsync(new MediaAssetId(request.AssetId), cancellationToken)
  ?? throw new NotFoundException(nameof(MediaAsset), request.AssetId);

        if (request.AltText is not null)
   asset.UpdateAltText(request.AltText);

   if (request.Tags is not null)
      asset.SetTags(request.Tags);

     repo.Update(asset);
     return Result.Success(MediaMapper.ToDto(asset));
    }
}

internal sealed class DeleteMediaAssetCommandHandler(
    IRepository<MediaAsset, MediaAssetId> repo,
    ICurrentUser currentUser)
    : IRequestHandler<DeleteMediaAssetCommand, Result>
{
 public async Task<Result> Handle(DeleteMediaAssetCommand request, CancellationToken cancellationToken)
    {
    var asset = await repo.GetByIdAsync(new MediaAssetId(request.AssetId), cancellationToken)
     ?? throw new NotFoundException(nameof(MediaAsset), request.AssetId);

      asset.Delete(currentUser.UserId);
    repo.Update(asset);
      return Result.Success();
    }
}

internal sealed class GetMediaAssetQueryHandler(
    IRepository<MediaAsset, MediaAssetId> repo)
    : IRequestHandler<GetMediaAssetQuery, Result<MediaAssetDto>>
{
    public async Task<Result<MediaAssetDto>> Handle(GetMediaAssetQuery request, CancellationToken cancellationToken)
    {
     var asset = await repo.GetByIdAsync(new MediaAssetId(request.AssetId), cancellationToken)
  ?? throw new NotFoundException(nameof(MediaAsset), request.AssetId);
  return Result.Success(MediaMapper.ToDto(asset));
    }
}

internal sealed class ListMediaAssetsQueryHandler(
    IRepository<MediaAsset, MediaAssetId> repo)
    : IRequestHandler<ListMediaAssetsQuery, Result<PagedList<MediaAssetListItemDto>>>
{
    public async Task<Result<PagedList<MediaAssetListItemDto>>> Handle(ListMediaAssetsQuery request, CancellationToken cancellationToken)
    {
var siteId = new SiteId(request.SiteId);
     var items = await repo.ListAsync(new MediaAssetsBySitePagedSpec(siteId, request.Page, request.PageSize), cancellationToken);
     var total = await repo.CountAsync(new MediaAssetsBySiteSpec(siteId), cancellationToken);

     return Result.Success(PagedList<MediaAssetListItemDto>.Create(
      items.Select(MediaMapper.ToListItemDto), request.Page, request.PageSize, total));
    }
}
