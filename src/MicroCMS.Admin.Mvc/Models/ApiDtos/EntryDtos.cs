namespace MicroCMS.Admin.Mvc.Models.ApiDtos;

public sealed class EntryListItem
{
    public string Id { get; init; } = string.Empty;
    public string SiteId { get; init; } = string.Empty;
    public string ContentTypeId { get; init; } = string.Empty;
    public string? ContentTypeName { get; init; }
    public string Slug { get; init; } = string.Empty;
    public string? Title { get; init; }
    public string Locale { get; init; } = string.Empty;
    public string AuthorId { get; init; } = string.Empty;
    public string? AuthorName { get; init; }
    public string Status { get; init; } = string.Empty;
    public int CurrentVersionNumber { get; init; }
    public string CreatedAt { get; init; } = string.Empty;
    public string UpdatedAt { get; init; } = string.Empty;
    public string? PublishedAt { get; init; }
}

public sealed class EntryDto
{
    public string Id { get; init; } = string.Empty;
    public string TenantId { get; init; } = string.Empty;
    public string SiteId { get; init; } = string.Empty;
    public string ContentTypeId { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public string Locale { get; init; } = string.Empty;
    public string AuthorId { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public int CurrentVersionNumber { get; init; }
    public Dictionary<string, object?> Fields { get; init; } = [];
    public string CreatedAt { get; init; } = string.Empty;
    public string UpdatedAt { get; init; } = string.Empty;
    public string? PublishedAt { get; init; }
}

public sealed class CreateEntryRequest
{
    public string SiteId { get; set; } = string.Empty;
    public string ContentTypeId { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Locale { get; set; } = string.Empty;
}

public sealed class UpdateEntryRequest
{
    public Dictionary<string, object?> Fields { get; set; } = [];
    public string? NewSlug { get; set; }
    public string? ChangeNote { get; set; }
}

public sealed class EntryListParams
{
    public string? SiteId { get; set; }
    public string? ContentTypeId { get; set; }
    public string? Status { get; set; }
    public string? Locale { get; set; }
    public string? Search { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
