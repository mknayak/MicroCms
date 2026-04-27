namespace MicroCMS.Application.Features.ContentTypes.Dtos;

public sealed record ContentTypeDto(
    Guid Id,
    Guid TenantId,
    Guid SiteId,
    string Handle,
    string DisplayName,
    string? Description,
    string LocalizationMode,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    IReadOnlyList<FieldDefinitionDto> Fields);

public sealed record FieldDefinitionDto(
    Guid Id,
    string Handle,
    string Label,
    string FieldType,
    bool IsRequired,
    bool IsLocalized,
    bool IsUnique,
    bool IsIndexed,
    int SortOrder,
    string? Description,
    /// <summary>Allowed values for Enum-type fields. Null for all other field types.</summary>
    IReadOnlyList<string>? Options = null);

public sealed record ContentTypeListItemDto(
    Guid Id,
    string Handle,
    string DisplayName,
    string Status,
    string LocalizationMode,
    int FieldCount,
    int EntryCount,
    int LocaleCount,
    DateTimeOffset UpdatedAt);
