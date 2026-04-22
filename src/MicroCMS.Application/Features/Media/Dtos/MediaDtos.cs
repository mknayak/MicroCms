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
    DateTimeOffset UpdatedAt);

public sealed record MediaAssetListItemDto(
    Guid Id,
    string FileName,
    string MimeType,
    long SizeBytes,
    string Status,
    string? AltText,
    string? AiAltText,
    DateTimeOffset CreatedAt);

public sealed record MediaFolderDto(
    Guid Id,
    Guid TenantId,
    Guid SiteId,
    string Name,
    Guid? ParentId);
