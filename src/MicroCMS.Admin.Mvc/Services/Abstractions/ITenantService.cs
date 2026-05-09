using MicroCMS.Admin.Mvc.Models.ApiDtos;

namespace MicroCMS.Admin.Mvc.Services.Abstractions;

public interface ITenantService
{
    Task<PagedResult<TenantListItem>> ListAsync(int page = 1, int pageSize = 20, CancellationToken ct = default);
    Task<TenantDetail> GetByIdAsync(string id, CancellationToken ct = default);
    Task<TenantDetail> OnboardAsync(OnboardTenantRequest request, CancellationToken ct = default);
    Task UpdateAsync(string id, UpdateTenantSettingsRequest request, CancellationToken ct = default);
    Task<SiteDto> CreateSiteAsync(string tenantId, CreateSiteRequest request, CancellationToken ct = default);
}
