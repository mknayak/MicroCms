namespace MicroCMS.GraphQL.Types;

// ── Entry inputs ───────────────────────────────────────────────────────────

/// <summary>Input for creating a new entry.</summary>
public sealed record CreateEntryInput(
    Guid SiteId,
 Guid ContentTypeId,
    string Slug,
    string Locale,
    string? FieldsJson);

/// <summary>Input for updating an existing entry.</summary>
public sealed record UpdateEntryInput(
    Guid EntryId,
    string FieldsJson,
  string? NewSlug = null,
    string? ChangeNote = null);

// ── ContentType inputs ─────────────────────────────────────────────────────

/// <summary>Input for creating a new content type.</summary>
public sealed record CreateContentTypeInput(
    Guid SiteId,
    string Handle,
    string DisplayName,
    string? Description = null);

/// <summary>Input for adding a field to a content type.</summary>
public sealed record AddFieldInput(
    Guid ContentTypeId,
  string Handle,
    string Label,
    string FieldType,
    bool IsRequired = false,
    bool IsLocalized = false,
    bool IsUnique = false,
    string? Description = null);
