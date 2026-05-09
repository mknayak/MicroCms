namespace MicroCMS.Admin.Mvc.Models.ApiDtos;

public sealed class MediaAsset
{
    public string Id { get; init; } = string.Empty;
    public string FileName { get; init; } = string.Empty;
    public string ContentType { get; init; } = string.Empty;
    public string MediaType { get; init; } = string.Empty;
    public string? Status { get; init; }
    public string Url { get; init; } = string.Empty;
    public string? ThumbnailUrl { get; init; }
    public long FileSize { get; init; }
    public int? Width { get; init; }
    public int? Height { get; init; }
    public string? AltText { get; init; }
    public List<string> Tags { get; init; } = [];
    public string? FolderId { get; init; }
    public string? UploadedByName { get; init; }
    public string CreatedAt { get; init; } = string.Empty;
}

public sealed class MediaFolder
{
    public string Id { get; init; } = string.Empty;
    public string SiteId { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string? ParentFolderId { get; init; }
    public int ChildCount { get; init; }
    public int AssetCount { get; init; }
    public string CreatedAt { get; init; } = string.Empty;
}

public sealed class UpdateMediaAssetRequest
{
    public string? AltText { get; set; }
    public List<string>? Tags { get; set; }
    public string? FolderId { get; set; }
}

public sealed class MediaListParams
{
    public string? Search { get; set; }
    public string? MediaType { get; set; }
    public string? FolderId { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 24;
}
