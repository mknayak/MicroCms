namespace MicroCMS.Admin.Mvc.Models.ApiDtos;

/// <summary>Generic paged result returned by list endpoints.</summary>
public sealed class PagedResult<T>
{
    public List<T> Items { get; init; } = [];
    public int TotalCount { get; init; }
    public int PageNumber { get; init; }
    public int PageSize { get; init; }
    public int TotalPages { get; init; }
}

/// <summary>RFC 7807 problem details returned on API errors.</summary>
public sealed class ProblemDetails
{
    public string? Type { get; init; }
    public string? Title { get; init; }
    public int? Status { get; init; }
    public string? Detail { get; init; }
    public Dictionary<string, string[]>? Errors { get; init; }
}
