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
    /// <summary>Discriminates what this content type represents: Content, Page, or Component.</summary>
    string Kind,
    /// <summary>Layout assigned to this type when Kind == Page. Null otherwise.</summary>
    Guid? LayoutId,
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
    string Kind,
    int FieldCount,
    int EntryCount,
    int LocaleCount,
    DateTimeOffset UpdatedAt);
