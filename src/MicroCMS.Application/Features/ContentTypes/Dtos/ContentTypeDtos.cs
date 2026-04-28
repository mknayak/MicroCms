using MicroCMS.Domain.Aggregates.Content;

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
    string Kind,
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
    bool IsList,
    int SortOrder,
    string? Description,
    /// <summary>
    /// Static option list for Enum fields.
    /// Null when the field uses a dynamic source or is not Enum.
    /// </summary>
    IReadOnlyList<string>? Options = null,
    /// <summary>
    /// Dynamic source config for Enum fields.
    /// When set, the entry editor must query published entries of the referenced
    /// content type to build the option list at render time.
    /// </summary>
    FieldDynamicSource? DynamicSource = null);

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
