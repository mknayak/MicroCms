namespace MicroCMS.Application.Features.Components.Dtos;

public sealed record ComponentFieldDto(
    Guid Id,
    string Handle,
    string Label,
    string FieldType,
    bool IsRequired,
    bool IsLocalized,
    bool IsUnique,
    bool IsIndexed,
    bool IsList,
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
    int UsageCount,
    int ItemCount,
    string TemplateType,
    string? TemplateContent,
    string? ThumbnailDataUri,
    IReadOnlyList<ComponentFieldDto> Fields,
 DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record ComponentListItemDto(
    Guid Id,
    string Name,
    string Key,
    string? Description,
    string Category,
    int UsageCount,
    int ItemCount,
    int FieldCount,
    string TemplateType,
    string? ThumbnailDataUri,
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
