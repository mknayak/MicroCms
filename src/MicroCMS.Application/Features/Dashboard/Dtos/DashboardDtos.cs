namespace MicroCMS.Application.Features.Dashboard.Dtos;

/// <summary>Aggregate statistics for the admin dashboard.</summary>
public sealed record DashboardStatsDto(
    int TotalEntries,
    int PublishedEntries,
    int DraftEntries,
    int TotalAssets,
    int TotalUsers,
    int ContentTypes);

/// <summary>A single activity item derived from recent entry mutations.</summary>
public sealed record DashboardActivityItemDto(
    Guid Id,
    string Type,
    string Description,
    string ActorName,
    string? ActorAvatarUrl,
    Guid EntityId,
    string EntityType,
    string EntityTitle,
    DateTimeOffset CreatedAt);
