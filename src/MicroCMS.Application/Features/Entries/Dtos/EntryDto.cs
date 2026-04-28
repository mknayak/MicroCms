using System.Text.Json;

namespace MicroCMS.Application.Features.Entries.Dtos;

/// <summary>
/// Full representation of an <see cref="MicroCMS.Domain.Aggregates.Content.Entry"/>.
/// <para>
/// <c>Fields</c> is serialised as a nested JSON object (not a raw string) so that
/// API consumers — including the admin SPA — receive a strongly-typed key/value map.
/// The domain stores <c>FieldsJson</c> internally as a string; the mapper deserialises it here.
/// </para>
/// </summary>
public sealed record EntryDto(
    Guid Id,
    Guid TenantId,
    Guid SiteId,
    Guid ContentTypeId,
    string Slug,
    string Locale,
    Guid AuthorId,
    string Status,
    int CurrentVersionNumber,
    /// <summary>Parsed field values. Null only when the stored JSON is invalid or empty.</summary>
    Dictionary<string, JsonElement>? Fields,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? PublishedAt,
    DateTimeOffset? ScheduledPublishAt,
    DateTimeOffset? ScheduledUnpublishAt,
    Guid? FolderId,
    /// <summary>All locale codes for which a variant of this entry (same site+contentType+slug) exists.</summary>
    IReadOnlyList<string>? LocaleVariants = null);
