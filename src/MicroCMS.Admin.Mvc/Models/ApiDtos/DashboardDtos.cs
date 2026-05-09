namespace MicroCMS.Admin.Mvc.Models.ApiDtos;

public sealed class DashboardStats
{
    public int TotalEntries { get; init; }
    public int PublishedEntries { get; init; }
    public int DraftEntries { get; init; }
    public int TotalAssets { get; init; }
    public int TotalUsers { get; init; }
    public int ContentTypes { get; init; }
}

public sealed class ActivityItem
{
    public string Id { get; init; } = string.Empty;
    public string Type { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string ActorName { get; init; } = string.Empty;
    public string? ActorAvatarUrl { get; init; }
    public string EntityId { get; init; } = string.Empty;
    public string EntityType { get; init; } = string.Empty;
    public string EntityTitle { get; init; } = string.Empty;
    public string CreatedAt { get; init; } = string.Empty;
}
