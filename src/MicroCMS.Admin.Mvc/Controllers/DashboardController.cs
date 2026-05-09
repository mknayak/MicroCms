using MicroCMS.Admin.Mvc.Infrastructure;
using MicroCMS.Admin.Mvc.Models.ViewModels.Dashboard;
using MicroCMS.Admin.Mvc.Services.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace MicroCMS.Admin.Mvc.Controllers;

/// <summary>
/// Renders the admin dashboard with summary statistics and recent activity.
/// </summary>
public sealed class DashboardController : BaseAdminController
{
    private readonly IDashboardService _dashboardService;
    private readonly ILogger<DashboardController> _logger;

    public DashboardController(IDashboardService dashboardService, ILogger<DashboardController> logger)
    {
        _dashboardService = dashboardService;
        _logger = logger;
    }

    public async Task<IActionResult> Index(CancellationToken ct)
    {
        try
        {
            var statsTask = _dashboardService.GetStatsAsync(ct);
            var activityTask = _dashboardService.GetActivityAsync(pageSize: 10, ct);

            await Task.WhenAll(statsTask, activityTask);

            var model = new DashboardViewModel
            {
                Stats = statsTask.Result,
                RecentActivity = activityTask.Result.Items,
            };

            return View(model);
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "Failed to load dashboard data.");
            AddError("Unable to load dashboard. Please refresh.");
            return View(new DashboardViewModel());
        }
    }
}
