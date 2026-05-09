using MicroCMS.Admin.Mvc.Infrastructure;
using MicroCMS.Admin.Mvc.Models.ApiDtos;
using MicroCMS.Admin.Mvc.Services.Abstractions;

namespace MicroCMS.Admin.Mvc.Services;

public sealed class TenantService : ApiClientBase, ITenantService
{
    public TenantService(IHttpClientFactory httpClientFactory) : base(httpClientFactory) { }

    public Task<PagedResult<TenantListItem>> ListAsync(int page = 1, int pageSize = 20, CancellationToken ct = default) =>
        GetAsync<PagedResult<TenantListItem>>($"admin/tenants?pageNumber={page}&pageSize={pageSize}", ct);

    public Task<TenantDetail> GetByIdAsync(string id, CancellationToken ct = default) =>
        GetAsync<TenantDetail>($"admin/tenants/{id}", ct);

    public Task<TenantDetail> OnboardAsync(OnboardTenantRequest request, CancellationToken ct = default) =>
        PostAsync<TenantDetail>("admin/tenants/onboard", request, ct);

    public Task UpdateAsync(string id, UpdateTenantSettingsRequest request, CancellationToken ct = default) =>
        PutAsync($"admin/tenants/{id}", request, ct);

    public Task<SiteDto> CreateSiteAsync(string tenantId, CreateSiteRequest request, CancellationToken ct = default) =>
        PostAsync<SiteDto>($"admin/tenants/{tenantId}/sites", request, ct);
}
