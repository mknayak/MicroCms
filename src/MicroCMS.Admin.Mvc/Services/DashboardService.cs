using MicroCMS.Admin.Mvc.Infrastructure;
using MicroCMS.Admin.Mvc.Models.ApiDtos;
using MicroCMS.Admin.Mvc.Services.Abstractions;

namespace MicroCMS.Admin.Mvc.Services;

public sealed class DashboardService : ApiClientBase, IDashboardService
{
    public DashboardService(IHttpClientFactory httpClientFactory) : base(httpClientFactory) { }

    public Task<DashboardStats> GetStatsAsync(CancellationToken ct = default) =>
        GetAsync<DashboardStats>("admin/dashboard/stats", ct);

    public Task<PagedResult<ActivityItem>> GetActivityAsync(int pageSize = 10, CancellationToken ct = default) =>
        GetAsync<PagedResult<ActivityItem>>($"admin/dashboard/activity?pageSize={pageSize}", ct);
}
