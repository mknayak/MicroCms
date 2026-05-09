using MicroCMS.Admin.Mvc.Models.ApiDtos;

namespace MicroCMS.Admin.Mvc.Models.ViewModels.ContentTypes;

public sealed class ContentTypeDetailViewModel
{
    public ContentTypeDto ContentType { get; init; } = new();
    public List<EntryListItem> RecentEntries { get; init; } = [];

    /// <summary>Total entry count returned by the API — used in tab badges and header.</summary>
    public int EntryCount { get; init; }
}
