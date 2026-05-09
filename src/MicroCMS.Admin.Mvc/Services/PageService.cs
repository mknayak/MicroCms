using MicroCMS.Admin.Mvc.Infrastructure;
using MicroCMS.Admin.Mvc.Models.ApiDtos;
using MicroCMS.Admin.Mvc.Services.Abstractions;

namespace MicroCMS.Admin.Mvc.Services;

public sealed class PageService : ApiClientBase, IPageService
{
    public PageService(IHttpClientFactory httpClientFactory) : base(httpClientFactory) { }

    public Task<List<PageTreeNode>> GetTreeAsync(CancellationToken ct = default) =>
        GetAsync<List<PageTreeNode>>("pages/tree", ct);

    public Task<PageDto> GetByIdAsync(string id, CancellationToken ct = default) =>
        GetAsync<PageDto>($"pages/{id}", ct);

    public Task<PageDto> CreateStaticPageAsync(CreateStaticPageRequest request, CancellationToken ct = default) =>
        PostAsync<PageDto>("pages/static", request, ct);

    public Task DeleteAsync(string id, CancellationToken ct = default) =>
        DeleteAsync($"pages/{id}", ct);
}
