using MicroCMS.Admin.Mvc.Models.ApiDtos;

namespace MicroCMS.Admin.Mvc.Services.Abstractions;

public interface ISearchService
{
    Task<SearchResults> SearchAsync(string query, int page = 1, int pageSize = 20, CancellationToken ct = default);
}
