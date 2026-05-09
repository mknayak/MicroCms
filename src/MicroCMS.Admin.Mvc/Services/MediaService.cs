using MicroCMS.Admin.Mvc.Infrastructure;
using MicroCMS.Admin.Mvc.Models.ApiDtos;
using MicroCMS.Admin.Mvc.Services.Abstractions;

namespace MicroCMS.Admin.Mvc.Services;

public sealed class MediaService : ApiClientBase, IMediaService
{
    public MediaService(IHttpClientFactory httpClientFactory) : base(httpClientFactory) { }

    public Task<PagedResult<MediaAsset>> ListAsync(MediaListParams parameters, CancellationToken ct = default)
    {
        var qs = BuildQueryString(parameters);
        return GetAsync<PagedResult<MediaAsset>>($"media?{qs}", ct);
    }

    public Task<MediaAsset> GetByIdAsync(string id, CancellationToken ct = default) =>
        GetAsync<MediaAsset>($"media/{id}", ct);

    public async Task<MediaAsset> UploadAsync(Stream fileStream, string fileName, string contentType, string? altText = null, string? folderId = null, CancellationToken ct = default)
    {
        using var form = new MultipartFormDataContent();
        var streamContent = new StreamContent(fileStream);
        streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
        form.Add(streamContent, "file", fileName);

        if (!string.IsNullOrWhiteSpace(altText))
        {
            form.Add(new StringContent(altText), "altText");
        }

        if (!string.IsNullOrWhiteSpace(folderId))
        {
            form.Add(new StringContent(folderId), "folderId");
        }

        return await PostMultipartAsync<MediaAsset>("media/upload", form, ct);
    }

    public Task<MediaAsset> UpdateAsync(string id, UpdateMediaAssetRequest request, CancellationToken ct = default) =>
        PutAsync<MediaAsset>($"media/{id}/metadata", request, ct);

    public Task DeleteAsync(string id, CancellationToken ct = default) =>
        DeleteAsync($"media/{id}", ct);

    public Task<List<MediaFolder>> ListFoldersAsync(string? parentFolderId = null, CancellationToken ct = default)
    {
        var path = string.IsNullOrWhiteSpace(parentFolderId)
            ? "media/folders"
            : $"media/folders?parentFolderId={Uri.EscapeDataString(parentFolderId)}";
        return GetAsync<List<MediaFolder>>(path, ct);
    }

    public Task<MediaFolder> CreateFolderAsync(string name, string? parentFolderId = null, CancellationToken ct = default) =>
        PostAsync<MediaFolder>("media/folders", new { name, parentFolderId }, ct);

    public Task DeleteFolderAsync(string id, CancellationToken ct = default) =>
        DeleteAsync($"media/folders/{id}", ct);

    private static string BuildQueryString(MediaListParams p)
    {
        var parts = new List<string>
        {
            $"page={p.Page}",
            $"pageSize={p.PageSize}",
        };

        if (!string.IsNullOrWhiteSpace(p.FolderId)) parts.Add($"folderId={Uri.EscapeDataString(p.FolderId)}");
        if (!string.IsNullOrWhiteSpace(p.Search)) parts.Add($"search={Uri.EscapeDataString(p.Search)}");
        if (!string.IsNullOrWhiteSpace(p.MediaType)) parts.Add($"mediaType={Uri.EscapeDataString(p.MediaType)}");

        return string.Join("&", parts);
    }
}
