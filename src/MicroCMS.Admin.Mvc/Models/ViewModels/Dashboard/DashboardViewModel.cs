using MicroCMS.Admin.Mvc.Models.ApiDtos;

namespace MicroCMS.Admin.Mvc.Models.ViewModels.Dashboard;

public sealed class DashboardViewModel
{
    public DashboardStats Stats { get; init; } = new();
    public List<ActivityItem> RecentActivity { get; init; } = [];
}
