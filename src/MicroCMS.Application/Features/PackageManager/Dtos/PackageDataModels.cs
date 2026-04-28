using System.Text.Json;

namespace MicroCMS.Application.Features.PackageManager.Dtos;

// ─── Package data records ─────────────────────────────────────────────────────
// These are the serialised representations written into the ZIP archive files.

public sealed record ContentTypePackageData(
    Guid Id,
    string Handle,
    string DisplayName,
    string? Description,
    string LocalizationMode,
    string Status,
    string Kind,
    Guid? LayoutId,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    IReadOnlyList<FieldPackageData> Fields);

public sealed record FieldPackageData(
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
    string? ValidationJson);

public sealed record EntryPackageData(
    Guid Id,
    Guid ContentTypeId,
 string ContentTypeHandle,
    string Slug,
    string Locale,
    string Status,
    int CurrentVersionNumber,
    string? FieldsJson,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? PublishedAt);

public sealed record PagePackageData(
    Guid Id,
    string Title,
    string Slug,
    string PageType,
    Guid? ParentId,
    int Depth,
    Guid? LinkedEntryId,
    Guid? CollectionContentTypeId,
    string? RoutePattern,
    Guid? LayoutId,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record LayoutPackageData(
    Guid Id,
    string Name,
    string Key,
    string TemplateType,
    bool IsDefault,
    string ZonesJson,
    string DefaultPlacementsJson,
    string? ShellTemplate,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record MediaMetadataPackageData(
    Guid Id,
    string FileName,
    string ContentType,
    string MediaType,
    string Url,
    long FileSize,
    int? Width,
    int? Height,
    string? AltText,
    IReadOnlyList<string> Tags,
    DateTimeOffset CreatedAt);

public sealed record ComponentPackageData(
    Guid Id,
    string Name,
    string Key,
    string? Description,
    string Category,
    IReadOnlyList<string> Zones,
    string TemplateType,
    string? TemplateContent,
    IReadOnlyList<ComponentFieldPackageData> Fields,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record ComponentFieldPackageData(
    Guid Id,
    string Handle,
    string Label,
    string FieldType,
    bool IsRequired,
    bool IsLocalized,
    bool IsIndexed,
    int SortOrder,
    string? Description);

public sealed record UserPackageData(
    Guid Id,
    string Email,
    string DisplayName,
    IReadOnlyList<string> Roles,
 bool IsActive,
    DateTimeOffset CreatedAt);

public sealed record SiteSettingsPackageData(
    Guid Id,
    string Name,
    string Handle,
 string DefaultLocale,
    bool IsActive,
    string? CustomDomain);
