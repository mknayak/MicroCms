using MicroCMS.Admin.Mvc.Infrastructure;
using MicroCMS.Admin.Mvc.Models.ApiDtos;
using MicroCMS.Admin.Mvc.Services.Abstractions;

namespace MicroCMS.Admin.Mvc.Services;

public sealed class ComponentService : ApiClientBase, IComponentService
{
    public ComponentService(IHttpClientFactory httpClientFactory) : base(httpClientFactory) { }

    public Task<PagedResult<ComponentListItem>> ListAsync(int page = 1, int pageSize = 20, CancellationToken ct = default) =>
        GetAsync<PagedResult<ComponentListItem>>($"components?pageNumber={page}&pageSize={pageSize}", ct);

    public Task<ComponentDto> GetByIdAsync(string id, CancellationToken ct = default) =>
        GetAsync<ComponentDto>($"components/{id}", ct);

    public Task<ComponentDto> CreateAsync(CreateComponentRequest request, CancellationToken ct = default) =>
        PostAsync<ComponentDto>("components", request, ct);

    public Task DeleteAsync(string id, CancellationToken ct = default) =>
        DeleteAsync($"components/{id}", ct);

    public Task<PagedResult<ComponentItemDto>> ListItemsAsync(string componentId, int page = 1, int pageSize = 20, CancellationToken ct = default) =>
        GetAsync<PagedResult<ComponentItemDto>>($"components/{componentId}/items?pageNumber={page}&pageSize={pageSize}", ct);
}
