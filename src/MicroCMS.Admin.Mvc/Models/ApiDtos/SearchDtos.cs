namespace MicroCMS.Admin.Mvc.Models.ApiDtos;

public sealed class SearchHit
{
    public string EntryId { get; init; } = string.Empty;
    public string SiteId { get; init; } = string.Empty;
    public string ContentTypeId { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public string Locale { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string? Title { get; init; }
    public string? Excerpt { get; init; }
    public double Score { get; init; }
    public string? PublishedAt { get; init; }
}

public sealed class SearchResults
{
    public List<SearchHit> Hits { get; init; } = [];
    public int TotalCount { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
}
