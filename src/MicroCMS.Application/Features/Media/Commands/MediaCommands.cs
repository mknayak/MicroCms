using MicroCMS.Application.Common.Attributes;
using MicroCMS.Application.Common.Authorization;
using MicroCMS.Application.Common.Markers;
using MicroCMS.Application.Features.Media.Dtos;
using MicroCMS.Shared.Primitives;

namespace MicroCMS.Application.Features.Media.Commands;

/// <summary>Registers a new media asset record (after the file has been stored by the storage adapter).</summary>
[HasPolicy(ContentPolicies.MediaUpload)]
public sealed record RegisterMediaAssetCommand(
  Guid SiteId,
    string FileName,
    string MimeType,
    long SizeBytes,
    string StorageKey,
  Guid? FolderId = null,
    int? WidthPx = null,
    int? HeightPx = null) : ICommand<MediaAssetDto>;

[HasPolicy(ContentPolicies.MediaUpload)]
public sealed record UpdateMediaAssetMetadataCommand(
    Guid AssetId,
    string? AltText,
    IReadOnlyList<string>? Tags) : ICommand<MediaAssetDto>;

[HasPolicy(ContentPolicies.MediaDelete)]
public sealed record DeleteMediaAssetCommand(Guid AssetId) : ICommand;
