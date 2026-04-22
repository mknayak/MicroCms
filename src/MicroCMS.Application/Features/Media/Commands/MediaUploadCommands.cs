using MicroCMS.Application.Common.Attributes;
using MicroCMS.Application.Common.Authorization;
using MicroCMS.Application.Common.Interfaces;
using MicroCMS.Application.Common.Markers;
using MicroCMS.Application.Features.Media.Dtos;
using MicroCMS.Shared.Primitives;

namespace MicroCMS.Application.Features.Media.Commands;

/// <summary>
/// Streams a raw file upload, detects its true MIME type, writes it to the configured
/// storage provider, and registers the resulting <see cref="MediaAssetDto"/> in the database.
/// The asset starts in <c>PendingScan</c> status; the background scan job transitions it to
/// <c>Available</c> or <c>Quarantined</c>.
/// </summary>
[HasPolicy(ContentPolicies.MediaUpload)]
public sealed record UploadMediaAssetCommand(
    Guid SiteId,
    string FileName,
    Stream Content,
    long ContentLength,
    string ClientMimeType,
    Guid? FolderId = null) : ICommand<MediaAssetDto>;

/// <summary>Generates a time-limited signed delivery URL for a private media asset.</summary>
[HasPolicy(ContentPolicies.MediaRead)]
public sealed record GetSignedUrlCommand(
    Guid AssetId,
    TimeSpan? ExpiresIn = null) : ICommand<SignedUrlDto>;

/// <summary>Moves a set of assets to a different (or null = root) media folder.</summary>
[HasPolicy(ContentPolicies.MediaUpload)]
public sealed record BulkMoveMediaCommand(
    IReadOnlyList<Guid> AssetIds,
    Guid? TargetFolderId) : ICommand;

/// <summary>Soft-deletes multiple media assets in one operation.</summary>
[HasPolicy(ContentPolicies.MediaDelete)]
public sealed record BulkDeleteMediaCommand(
    IReadOnlyList<Guid> AssetIds) : ICommand;

/// <summary>Replaces the tag list on multiple assets atomically.</summary>
[HasPolicy(ContentPolicies.MediaUpload)]
public sealed record BulkRetagMediaCommand(
    IReadOnlyList<Guid> AssetIds,
    IReadOnlyList<string> Tags) : ICommand;
