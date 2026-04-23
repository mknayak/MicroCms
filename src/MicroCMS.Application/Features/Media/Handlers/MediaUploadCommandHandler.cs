using MediatR;
using MicroCMS.Application.Common.Exceptions;
using MicroCMS.Application.Common.Interfaces;
using MicroCMS.Application.Features.Media.Commands;
using MicroCMS.Application.Features.Media.Dtos;
using MicroCMS.Application.Features.Media.Mappers;
using MicroCMS.Domain.Aggregates.Media;
using MicroCMS.Domain.Repositories;
using MicroCMS.Domain.ValueObjects;
using MicroCMS.Shared.Ids;
using MicroCMS.Shared.Results;

namespace MicroCMS.Application.Features.Media.Handlers;

/// <summary>
/// Handles the full upload pipeline:
///   1. Detect true MIME type from magic bytes.
///   2. Validate file size against domain limits.
///   3. Write binary to storage provider.
///   4. Persist <see cref="MediaAsset"/> with status <c>PendingScan</c>.
///
/// The background <c>MediaScanJob</c> will advance the asset to <c>Available</c> or
/// <c>Quarantined</c> once ClamAV completes its scan.
/// </summary>
public sealed class UploadMediaAssetCommandHandler(
    IStorageProvider storageProvider,
    IMimeTypeInspector mimeInspector,
    IRepository<MediaAsset, MediaAssetId> repo,
    ICurrentUser currentUser) : IRequestHandler<UploadMediaAssetCommand, Result<MediaAssetDto>>
{
    public async Task<Result<MediaAssetDto>> Handle(
        UploadMediaAssetCommand request,
        CancellationToken cancellationToken)
    {
        if (request.ContentLength > AssetMetadata.MaxFileSizeBytes)
        {
            return Result.Failure<MediaAssetDto>(
                Error.Validation("Media.FileTooLarge",
                    $"File exceeds the maximum allowed size of {AssetMetadata.MaxFileSizeBytes / (1024 * 1024)} MB."));
        }

        var trueMimeType = await mimeInspector.DetectAsync(
            request.Content, request.FileName, cancellationToken);

        var storageKey = await storageProvider.UploadAsync(
            request.Content,
            request.FileName,
            trueMimeType,
            currentUser.TenantId.Value.ToString(),
            cancellationToken);

        var metadata = AssetMetadata.Create(
            request.FileName, trueMimeType, request.ContentLength);

        var asset = MediaAsset.Create(
            currentUser.TenantId,
            new SiteId(request.SiteId),
            metadata,
            storageKey,
            currentUser.UserId,
            request.FolderId);

        asset.MarkUploadComplete(); // Uploading → PendingScan

        await repo.AddAsync(asset, cancellationToken);
        return Result.Success(MediaMapper.ToDto(asset));
    }
}
