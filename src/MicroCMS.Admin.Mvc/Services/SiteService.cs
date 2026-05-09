using MicroCMS.Admin.Mvc.Infrastructure;
using MicroCMS.Admin.Mvc.Models.ApiDtos;
using MicroCMS.Admin.Mvc.Services.Abstractions;

namespace MicroCMS.Admin.Mvc.Services;

public sealed class SiteService : ApiClientBase, ISiteService
{
    public SiteService(IHttpClientFactory httpClientFactory) : base(httpClientFactory) { }

    public Task<SiteDetailDto> GetByIdAsync(string id, CancellationToken ct = default) =>
        GetAsync<SiteDetailDto>($"sites/{id}", ct);

    public Task<SiteSettingsDto> GetSettingsAsync(string id, CancellationToken ct = default) =>
        GetAsync<SiteSettingsDto>($"sites/{id}/settings", ct);

    public Task UpdateSettingsAsync(string id, UpdateSiteSettingsRequest request, CancellationToken ct = default) =>
        PutAsync($"sites/{id}/settings", request, ct);
}
