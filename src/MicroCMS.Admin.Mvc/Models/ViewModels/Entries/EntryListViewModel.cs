using MicroCMS.Admin.Mvc.Models.ApiDtos;

namespace MicroCMS.Admin.Mvc.Models.ViewModels.Entries;

public sealed class EntryListViewModel
{
    public List<EntryListItem> Items { get; init; } = [];
    public int TotalCount { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public int TotalPages { get; init; }
    public string? ContentTypeId { get; init; }
    public string? ContentTypeName { get; init; }
    public string? StatusFilter { get; init; }
    public string? SearchQuery { get; init; }
}
