using MicroCMS.Admin.Mvc.Infrastructure;
using MicroCMS.Admin.Mvc.Models.ApiDtos;
using MicroCMS.Admin.Mvc.Services.Abstractions;

namespace MicroCMS.Admin.Mvc.Services;

public sealed class LayoutService : ApiClientBase, ILayoutService
{
    public LayoutService(IHttpClientFactory httpClientFactory) : base(httpClientFactory) { }

    public Task<List<LayoutListItem>> ListAsync(CancellationToken ct = default) =>
        GetAsync<List<LayoutListItem>>("layouts", ct);

    public Task<LayoutDto> GetByIdAsync(string id, CancellationToken ct = default) =>
        GetAsync<LayoutDto>($"layouts/{id}", ct);

    public Task<LayoutDto> CreateAsync(CreateLayoutRequest request, CancellationToken ct = default) =>
        PostAsync<LayoutDto>("layouts", request, ct);

    public Task<LayoutDto> UpdateAsync(string id, UpdateLayoutRequest request, CancellationToken ct = default) =>
        PutAsync<LayoutDto>($"layouts/{id}", request, ct);

    public Task DeleteAsync(string id, CancellationToken ct = default) =>
        DeleteAsync($"layouts/{id}", ct);
}
