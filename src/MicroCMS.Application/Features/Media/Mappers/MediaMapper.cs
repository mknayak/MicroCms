using MicroCMS.Application.Features.Media.Dtos;
using MicroCMS.Domain.Aggregates.Media;

namespace MicroCMS.Application.Features.Media.Mappers;

public static class MediaMapper
{
    public static MediaFolderDto ToFolderDto(MediaFolder f) => new(
        f.Id,
        f.TenantId.Value,
        f.SiteId.Value,
        f.Name,
        f.ParentFolderId);

    public static MediaAssetDto ToDto(MediaAsset a) => new(
        a.Id.Value,
        a.TenantId.Value,
        a.SiteId.Value,
        a.Metadata.FileName,
        a.Metadata.MimeType,
        a.Metadata.SizeBytes,
        a.Metadata.WidthPx,
        a.Metadata.HeightPx,
        a.StorageKey,
        a.FolderId,
        a.Status.ToString(),
        a.AltText,
        a.AiAltText,
        a.Visibility.ToString(),
        a.Tags,
        a.CreatedAt,
        a.UpdatedAt);

    public static MediaAssetListItemDto ToListItemDto(MediaAsset a)
    {
        var mediaType = MediaTypeFromMime(a.Metadata.MimeType);
        var url = $"/api/v1/media/{a.Id.Value}/download";
        var thumbnailUrl = mediaType == "image"
            ? $"/api/v1/media/{a.Id.Value}/variant?w=400&h=400&fit=Cover&fmt=WebP&q=80"
            : null;

        return new MediaAssetListItemDto(
            a.Id.Value,
            a.Metadata.FileName,
            a.Metadata.MimeType,   // ContentType
            mediaType,
            a.Metadata.SizeBytes,  // FileSize
            a.Status.ToString(),
            a.AltText,
            a.AiAltText,
            url,
            thumbnailUrl,
            a.CreatedAt);
    }

    private static string MediaTypeFromMime(string mimeType) => mimeType switch
    {
        var m when m.StartsWith("image/",     StringComparison.OrdinalIgnoreCase) => "image",
        var m when m.StartsWith("video/",     StringComparison.OrdinalIgnoreCase) => "video",
        var m when m.StartsWith("audio/",     StringComparison.OrdinalIgnoreCase) => "audio",
        "application/pdf" => "document",
        var m when m.StartsWith("application/vnd.") || m.StartsWith("application/msword") => "document",
        _ => "other",
    };

    public static IReadOnlyList<MediaAssetDto> ToDtos(IEnumerable<MediaAsset> assets) =>
        assets.Select(ToDto).ToList().AsReadOnly();

    public static IReadOnlyList<MediaAssetListItemDto> ToListItemDtos(IEnumerable<MediaAsset> assets) =>
        assets.Select(ToListItemDto).ToList().AsReadOnly();
}
