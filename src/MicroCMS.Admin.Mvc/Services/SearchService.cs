using MicroCMS.Admin.Mvc.Infrastructure;
using MicroCMS.Admin.Mvc.Models.ApiDtos;
using MicroCMS.Admin.Mvc.Services.Abstractions;

namespace MicroCMS.Admin.Mvc.Services;

public sealed class SearchService : ApiClientBase, ISearchService
{
    public SearchService(IHttpClientFactory httpClientFactory) : base(httpClientFactory) { }

    public Task<SearchResults> SearchAsync(string query, int page = 1, int pageSize = 20, CancellationToken ct = default) =>
        GetAsync<SearchResults>($"search?query={Uri.EscapeDataString(query)}&page={page}&pageSize={pageSize}", ct);
}
