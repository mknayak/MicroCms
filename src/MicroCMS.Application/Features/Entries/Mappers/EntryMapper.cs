using System.Text.Json;
using MicroCMS.Application.Features.Entries.Dtos;
using MicroCMS.Domain.Aggregates.Content;

namespace MicroCMS.Application.Features.Entries.Mappers;

/// <summary>
/// Manual mapper for Entry → DTO conversions.
/// FieldsJson is deserialised into a <see cref="Dictionary{TKey,TValue}"/> of
/// <see cref="JsonElement"/> so that API consumers receive a structured JSON object
/// rather than a raw escaped string.
/// </summary>
public static class EntryMapper
{
    private static readonly JsonSerializerOptions _jsonOptions =
        new() { PropertyNameCaseInsensitive = true };

    /// <summary>Maps an <see cref="Entry"/> aggregate to its full DTO representation.</summary>
    public static EntryDto ToDto(Entry entry, IReadOnlyList<string>? localeVariants = null) => new(
        entry.Id.Value,
        entry.TenantId.Value,
        entry.SiteId.Value,
        entry.ContentTypeId.Value,
        entry.Slug.Value,
        entry.Locale.Value,
        entry.AuthorId,
        entry.Status.ToString(),
        entry.CurrentVersionNumber,
        DeserialiseFields(entry.FieldsJson),
        entry.CreatedAt,
        entry.UpdatedAt,
        entry.PublishedAt,
        entry.ScheduledPublishAt,
        entry.ScheduledUnpublishAt,
        entry.FolderId?.Value,
        localeVariants);

    /// <summary>
    /// Maps an <see cref="Entry"/> aggregate to a lightweight list item DTO.
    /// <c>Title</c> is extracted from <c>FieldsJson</c> by peeking at the "title" key.
    /// <c>ContentTypeName</c> and <c>AuthorName</c> are left null here;
    /// query handlers that perform joins should use <see cref="ToListItemDtoWithContext"/>.
    /// </summary>
    public static EntryListItemDto ToListItemDto(Entry entry) =>
        ToListItemDtoWithContext(entry, contentTypeName: null, authorName: null);

    /// <summary>Maps with additional context available from join projections.</summary>
    public static EntryListItemDto ToListItemDtoWithContext(
        Entry entry,
        string? contentTypeName,
        string? authorName)
    {
        var title = ExtractTitle(entry.FieldsJson);
        return new EntryListItemDto(
            entry.Id.Value,
            entry.SiteId.Value,
            entry.ContentTypeId.Value,
            contentTypeName,
            entry.Slug.Value,
            title,
            entry.Locale.Value,
            entry.AuthorId,
            authorName,
            entry.Status.ToString(),
            entry.CurrentVersionNumber,
            entry.CreatedAt,
            entry.UpdatedAt,
            entry.PublishedAt,
            entry.ScheduledPublishAt);
    }

    /// <summary>Maps an <see cref="EntryVersion"/> entity to its DTO.</summary>
    public static EntryVersionDto ToVersionDto(EntryVersion version) => new(
        version.Id,
        version.EntryId.Value,
        version.VersionNumber,
        DeserialiseFields(version.FieldsJson),
        version.AuthorId,
        version.ChangeNote,
        version.CreatedAt);

    /// <summary>Projects a sequence of entries to list item DTOs.</summary>
    public static IReadOnlyList<EntryListItemDto> ToListItemDtos(IEnumerable<Entry> entries) =>
        entries.Select(ToListItemDto).ToList().AsReadOnly();

    /// <summary>Projects a sequence of versions to version DTOs.</summary>
    public static IReadOnlyList<EntryVersionDto> ToVersionDtos(IEnumerable<EntryVersion> versions) =>
        versions.Select(ToVersionDto).ToList().AsReadOnly();

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static Dictionary<string, JsonElement>? DeserialiseFields(string fieldsJson)
    {
        if (string.IsNullOrWhiteSpace(fieldsJson) || fieldsJson == "{}")
            return new Dictionary<string, JsonElement>();

        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(fieldsJson, _jsonOptions);
        }
        catch (JsonException)
        {
            // Defensive: malformed JSON in DB should not crash the response
            return null;
        }
    }

    /// <summary>
    /// Attempts to extract a "title" value from FieldsJson without fully deserialising the document.
    /// Returns null when the field is absent or the value is not a string.
    /// </summary>
    private static string? ExtractTitle(string fieldsJson)
    {
        if (string.IsNullOrWhiteSpace(fieldsJson) || fieldsJson == "{}")
            return null;

        try
        {
            using var doc = JsonDocument.Parse(fieldsJson);
            if (doc.RootElement.TryGetProperty("title", out var titleEl) &&
                titleEl.ValueKind == JsonValueKind.String)
            {
                return titleEl.GetString();
            }
        }
        catch (JsonException)
        {
            // Swallow — title extraction is best-effort
        }

        return null;
    }
}
