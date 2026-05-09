using MicroCMS.Admin.Mvc.Models.ApiDtos;

namespace MicroCMS.Admin.Mvc.Models.ViewModels.Search;

public sealed class SearchViewModel
{
    public string Query { get; init; } = string.Empty;
    public List<SearchHit> Hits { get; init; } = [];
    public int TotalCount { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}
