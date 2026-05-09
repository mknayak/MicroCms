using MicroCMS.Admin.Mvc.Models.ApiDtos;

namespace MicroCMS.Admin.Mvc.Services.Abstractions;

public interface ISiteService
{
    Task<SiteDetailDto> GetByIdAsync(string id, CancellationToken ct = default);
    Task<SiteSettingsDto> GetSettingsAsync(string id, CancellationToken ct = default);
    Task UpdateSettingsAsync(string id, UpdateSiteSettingsRequest request, CancellationToken ct = default);
}
