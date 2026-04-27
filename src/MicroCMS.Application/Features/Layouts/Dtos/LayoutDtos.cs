using System.Text.Json.Serialization;

namespace MicroCMS.Application.Features.Layouts.Dtos;

public sealed record LayoutColumnDefDto(int Span, string ZoneName);

public sealed record LayoutZoneNodeDto(
    string Id,
    string Type,   // "zone" | "grid-row"
    string Name,
    string Label,
    int SortOrder,
    IReadOnlyList<LayoutColumnDefDto>? Columns = null);

public sealed record LayoutDefaultPlacementDto(
    Guid ComponentId,
    string ComponentName,
    string Zone,
    int SortOrder,
  bool IsLocked);

public sealed record LayoutDto(
    Guid Id,
    Guid TenantId,
    Guid SiteId,
    string Name,
    string Key,
    string TemplateType,
    string? ShellTemplate,
    bool IsDefault,
    IReadOnlyList<LayoutZoneNodeDto> Zones,
    IReadOnlyList<LayoutDefaultPlacementDto> DefaultPlacements,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record LayoutListItemDto(
    Guid Id,
    string Name,
    string Key,
    string TemplateType,
    bool IsDefault,
    int ZoneCount,
    DateTimeOffset UpdatedAt);
