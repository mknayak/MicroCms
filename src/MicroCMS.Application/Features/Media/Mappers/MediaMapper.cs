using MicroCMS.Application.Features.Media.Dtos;
using MicroCMS.Domain.Aggregates.Media;

namespace MicroCMS.Application.Features.Media.Mappers;

public static class MediaMapper
{
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
        a.Tags,
    a.CreatedAt,
     a.UpdatedAt);

    public static MediaAssetListItemDto ToListItemDto(MediaAsset a) => new(
        a.Id.Value,
    a.Metadata.FileName,
        a.Metadata.MimeType,
        a.Metadata.SizeBytes,
   a.Status.ToString(),
   a.AltText,
  a.CreatedAt);
}
