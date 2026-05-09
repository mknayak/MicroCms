using MicroCMS.Admin.Mvc.Models.ApiDtos;

namespace MicroCMS.Admin.Mvc.Services.Abstractions;

public interface IDashboardService
{
    Task<DashboardStats> GetStatsAsync(CancellationToken ct = default);
    Task<PagedResult<ActivityItem>> GetActivityAsync(int pageSize = 10, CancellationToken ct = default);
}
