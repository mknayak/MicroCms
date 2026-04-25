namespace MicroCMS.Application.Features.Components.Dtos;

public sealed record ComponentFieldDto(
    Guid Id,
    string Handle,
    string Label,
    string FieldType,
    bool IsRequired,
bool IsLocalized,
    bool IsIndexed,
    int SortOrder,
    string? Description);

public sealed record ComponentDto(
Guid Id,
    Guid TenantId,
    Guid SiteId,
    string Name,
    string Key,
    string? Description,
    string Category,
    IReadOnlyList<string> Zones,
    int UsageCount,
    int ItemCount,
    string TemplateType,
    string? TemplateContent,
    IReadOnlyList<ComponentFieldDto> Fields,
 DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record ComponentListItemDto(
    Guid Id,
    string Name,
    string Key,
    string? Description,
    string Category,
    IReadOnlyList<string> Zones,
    int UsageCount,
    int ItemCount,
    int FieldCount,
    string TemplateType,
    DateTimeOffset CreatedAt,
 DateTimeOffset UpdatedAt);

public sealed record ComponentItemDto(
    Guid Id,
    Guid ComponentId,
    string ComponentName,
    string ComponentKey,
    Guid TenantId,
    Guid SiteId,
    string Title,
    string Status,
    object FieldsJson,
    int UsedOnPages,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
