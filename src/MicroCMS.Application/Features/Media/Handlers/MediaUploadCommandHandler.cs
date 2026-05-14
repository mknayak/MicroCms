using MediatR;
using MicroCMS.Application.Common.Exceptions;
using MicroCMS.Application.Common.Interfaces;
using MicroCMS.Application.Features.Media.Commands;
using MicroCMS.Application.Features.Media.Dtos;
using MicroCMS.Application.Features.Media.Mappers;
using MicroCMS.Application.Features.Media.Options;
using MicroCMS.Domain.Aggregates.Media;
using MicroCMS.Domain.Repositories;
using MicroCMS.Domain.ValueObjects;
using MicroCMS.Shared.Ids;
using MicroCMS.Shared.Results;
using Microsoft.Extensions.Options;

namespace MicroCMS.Application.Features.Media.Handlers;

/// <summary>
/// Handles the full upload pipeline:
///   1. Detect true MIME type from magic bytes.
///   2. Validate file size against domain limits.
///   3. Write binary to storage provider.
///   4. Persist <see cref="MediaAsset"/> with status <c>PendingScan</c>, or
///      immediately <c>Available</c> when <see cref="MediaOptions.SkipVirusScan"/> is enabled.
///
/// When ClamAV is active the background <c>MediaScanJob</c> will advance the asset to
/// <c>Available</c> or <c>Quarantined</c> once the scan completes.
/// </summary>
public sealed class UploadMediaAssetCommandHandler(
    IStorageProvider storageProvider,
    IMimeTypeInspector mimeInspector,
    IRepository<MediaAsset, MediaAssetId> repo,
    ICurrentUser currentUser,
    IOptions<MediaOptions> mediaOptions) : IRequestHandler<UploadMediaAssetCommand, Result<MediaAssetDto>>
{
    private readonly MediaOptions _mediaOptions = mediaOptions.Value;
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

        // Buffer the multipart section body into a MemoryStream so that both the MIME
        // inspector and the storage provider read a complete, seekable copy of the bytes.
        // MultipartReader section bodies are forward-only; without buffering, DetectAsync
        // consumes the first 512 bytes and UploadAsync stores a truncated file.
        using var buffered = new MemoryStream();
        await request.Content.CopyToAsync(buffered, cancellationToken);
        buffered.Position = 0;

        var trueMimeType = await mimeInspector.DetectAsync(
            buffered, request.FileName, cancellationToken);

        buffered.Position = 0;

        var storageKey = await storageProvider.UploadAsync(
            buffered,
            request.FileName,
            trueMimeType,
            currentUser.TenantId.Value.ToString(),
            cancellationToken);

        var metadata = AssetMetadata.Create(
            request.FileName, trueMimeType, buffered.Length);

        var asset = MediaAsset.Create(
            currentUser.TenantId,
            currentUser.SiteId ?? new SiteId(Guid.Empty),
            metadata,
            storageKey,
            currentUser.UserId,
            request.FolderId);

        asset.MarkUploadComplete(); // Uploading → PendingScan

        if (_mediaOptions.SkipVirusScan)
            asset.MarkAvailable(); // PendingScan → Available (no ClamAV in this environment)

        await repo.AddAsync(asset, cancellationToken);
        return Result.Success(MediaMapper.ToDto(asset));
    }
}
