namespace MicroCMS.Application.Features.Media.Dtos;

public sealed record MediaAssetDto(
    Guid Id,
    Guid TenantId,
    Guid SiteId,
    string FileName,
    string MimeType,
    long SizeBytes,
    int? WidthPx,
    int? HeightPx,
    string StorageKey,
    Guid? FolderId,
    string Status,
    string? AltText,
    string? AiAltText,
    string Visibility,
    IReadOnlyList<string> Tags,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    string? AssetPath = null);

public sealed record MediaAssetListItemDto(
    Guid Id,
    string FileName,
    string ContentType,
    string MediaType,
    long FileSize,
    string Status,
    string? AltText,
    string? AiAltText,
    string Url,
    string? ThumbnailUrl,
    DateTimeOffset CreatedAt,
    Guid? FolderId = null,
    IReadOnlyList<string>? Tags = null,
    string? AssetPath = null);

public sealed record MediaFolderDto(
    Guid Id,
    Guid TenantId,
    Guid SiteId,
    string Name,
    Guid? ParentFolderId,
    int ChildCount = 0,
    int AssetCount = 0);
