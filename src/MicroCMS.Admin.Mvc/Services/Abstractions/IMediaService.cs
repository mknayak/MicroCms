using MicroCMS.Admin.Mvc.Models.ApiDtos;

namespace MicroCMS.Admin.Mvc.Services.Abstractions;

public interface IMediaService
{
    Task<PagedResult<MediaAsset>> ListAsync(MediaListParams parameters, CancellationToken ct = default);
    Task<MediaAsset> GetByIdAsync(string id, CancellationToken ct = default);
    Task<MediaAsset> UploadAsync(Stream fileStream, string fileName, string contentType, string? altText = null, string? folderId = null, CancellationToken ct = default);
    Task<MediaAsset> UpdateAsync(string id, UpdateMediaAssetRequest request, CancellationToken ct = default);
    Task DeleteAsync(string id, CancellationToken ct = default);
    Task<List<MediaFolder>> ListFoldersAsync(string? parentFolderId = null, CancellationToken ct = default);
    Task<MediaFolder> CreateFolderAsync(string name, string? parentFolderId = null, CancellationToken ct = default);
    Task DeleteFolderAsync(string id, CancellationToken ct = default);
}
