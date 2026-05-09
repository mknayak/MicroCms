using MicroCMS.Admin.Mvc.Models.ApiDtos;

namespace MicroCMS.Admin.Mvc.Models.ViewModels.Media;

public sealed class MediaViewModel
{
    public List<MediaAsset> Assets { get; init; } = [];
    public List<MediaFolder> Folders { get; init; } = [];
    public int TotalCount { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 24;
    public int TotalPages { get; init; }
    public string? CurrentFolderId { get; init; }
    public string? SearchQuery { get; init; }
    public string? MediaTypeFilter { get; init; }
}
