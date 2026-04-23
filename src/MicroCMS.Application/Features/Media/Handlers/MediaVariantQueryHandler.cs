using MediatR;
using MicroCMS.Application.Common.Exceptions;
using MicroCMS.Application.Common.Interfaces;
using MicroCMS.Application.Features.Media.Queries;
using MicroCMS.Domain.Aggregates.Media;
using MicroCMS.Domain.Repositories;
using MicroCMS.Shared.Ids;
using MicroCMS.Shared.Results;

namespace MicroCMS.Application.Features.Media.Handlers;

/// <summary>
/// Downloads the source image from storage and returns a transformed variant stream.
/// Supports resize/crop and format conversion (JPEG, PNG, WebP).
/// </summary>
internal sealed class GetImageVariantQueryHandler(
    IRepository<MediaAsset, MediaAssetId> assetRepo,
    IStorageProvider storageProvider,
    IImageVariantService variantService,
    ICurrentUser currentUser) : IRequestHandler<GetImageVariantQuery, Result<ImageVariantResult>>
{
    public async Task<Result<ImageVariantResult>> Handle(
        GetImageVariantQuery request,
        CancellationToken cancellationToken)
    {
        var asset = await assetRepo.GetByIdAsync(new MediaAssetId(request.AssetId), cancellationToken)
            ?? throw new NotFoundException(nameof(MediaAsset), request.AssetId);

        if (asset.TenantId != currentUser.TenantId)
            return Result.Failure<ImageVariantResult>(
                Error.Forbidden("Media.CrossTenantAccess", "Asset does not belong to your tenant."));

        if (!asset.Metadata.IsImage)
            return Result.Failure<ImageVariantResult>(
                Error.Validation("Media.NotAnImage", "Image variants are only supported for image assets."));

        var source = await storageProvider.DownloadAsync(asset.StorageKey, cancellationToken);

        var variantRequest = new ImageVariantRequest(
            request.Width,
            request.Height,
            request.Fit,
            request.Format,
            request.Quality);

        var output = await variantService.TransformAsync(source, variantRequest, cancellationToken);
        var mimeType = variantService.GetMimeType(request.Format, asset.Metadata.MimeType);

        return Result.Success(new ImageVariantResult(output, mimeType));
    }
}
