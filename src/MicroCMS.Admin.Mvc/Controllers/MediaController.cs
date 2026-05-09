using MicroCMS.Admin.Mvc.Infrastructure;
using MicroCMS.Admin.Mvc.Models.ApiDtos;
using MicroCMS.Admin.Mvc.Models.ViewModels.Media;
using MicroCMS.Admin.Mvc.Services.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace MicroCMS.Admin.Mvc.Controllers;

/// <summary>
/// Manages media assets: list, upload, folder management, and delete.
/// </summary>
public sealed class MediaController : BaseAdminController
{
    private const long MaxUploadSizeBytes = 50 * 1024 * 1024; // 50 MB

    private readonly IMediaService _mediaService;
    private readonly ILogger<MediaController> _logger;

    public MediaController(IMediaService mediaService, ILogger<MediaController> logger)
    {
        _mediaService = mediaService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        string? folderId,
        string? search,
        string? mediaType,
        int page = 1,
        CancellationToken ct = default)
    {
        try
        {
            var parameters = new MediaListParams
            {
                FolderId = folderId,
                Search = search,
                MediaType = mediaType,
                Page = page,
                PageSize = 24,
            };

            var assetsTask = _mediaService.ListAsync(parameters, ct);
            var foldersTask = _mediaService.ListFoldersAsync(folderId, ct);

            await Task.WhenAll(assetsTask, foldersTask);

            var model = new MediaViewModel
            {
                Assets = assetsTask.Result.Items,
                Folders = foldersTask.Result,
                TotalCount = assetsTask.Result.TotalCount,
                PageNumber = assetsTask.Result.PageNumber,
                PageSize = assetsTask.Result.PageSize,
                TotalPages = assetsTask.Result.TotalPages,
                CurrentFolderId = folderId,
                SearchQuery = search,
                MediaTypeFilter = mediaType,
            };

            return View(model);
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "Failed to load media library.");
            AddError("Unable to load media library.");
            return View(new MediaViewModel());
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Upload(IFormFile file, string? folderId, string? altText, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
        {
            AddError("Please select a file to upload.");
            return RedirectToAction(nameof(Index), new { folderId });
        }

        if (file.Length > MaxUploadSizeBytes)
        {
            AddError("File size exceeds the 50 MB limit.");
            return RedirectToAction(nameof(Index), new { folderId });
        }

        try
        {
            await using var stream = file.OpenReadStream();
            await _mediaService.UploadAsync(stream, file.FileName, file.ContentType, altText, folderId, ct);
            AddSuccess($"'{file.FileName}' uploaded successfully.");
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "Upload failed for file {FileName}.", file.FileName);
            AddError(ex.Message);
        }

        return RedirectToAction(nameof(Index), new { folderId });
    }

    /// <summary>
    /// JSON endpoint used by the asset-picker modal in the entry editor.
    /// Returns a paged list of media assets matching the search/type filter.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Picker(
        string? search,
        string? mediaType,
        int page = 1,
        CancellationToken ct = default)
    {
        try
        {
            var result = await _mediaService.ListAsync(new MediaListParams
            {
                Search = search,
                MediaType = mediaType,
                Page = page,
                PageSize = 18,
            }, ct);

            return Json(result, new System.Text.Json.JsonSerializerOptions
            {
                PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
            });
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "Failed to load media for picker.");
            return Json(new { items = Array.Empty<object>(), totalCount = 0 });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string id, string? folderId, CancellationToken ct)
    {
        try
        {
            await _mediaService.DeleteAsync(id, ct);
            AddSuccess("Asset deleted.");
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "Failed to delete asset {Id}.", id);
            AddError(ex.Message);
        }

        return RedirectToAction(nameof(Index), new { folderId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateFolder(string name, string? parentFolderId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            AddError("Folder name is required.");
            return RedirectToAction(nameof(Index), new { folderId = parentFolderId });
        }

        try
        {
            await _mediaService.CreateFolderAsync(name, parentFolderId, ct);
            AddSuccess($"Folder '{name}' created.");
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "Failed to create folder '{Name}'.", name);
            AddError(ex.Message);
        }

        return RedirectToAction(nameof(Index), new { folderId = parentFolderId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteFolder(string id, string? parentFolderId, CancellationToken ct)
    {
        try
        {
            await _mediaService.DeleteFolderAsync(id, ct);
            AddSuccess("Folder deleted.");
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "Failed to delete folder {Id}.", id);
            AddError(ex.Message);
        }

        return RedirectToAction(nameof(Index), new { folderId = parentFolderId });
    }
}
