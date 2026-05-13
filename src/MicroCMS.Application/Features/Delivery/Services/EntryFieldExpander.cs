using System.Text.Json;
using MicroCMS.Application.Common.Interfaces;
using MicroCMS.Domain.Aggregates.Content;
using MicroCMS.Domain.Aggregates.Media;
using MicroCMS.Domain.Enums;
using MicroCMS.Domain.Repositories;
using MicroCMS.Shared.Ids;

namespace MicroCMS.Application.Features.Delivery.Services;

/// <summary>
/// Expands typed field values in an entry's <c>FieldsJson</c> before rendering:
/// <list type="bullet">
///   <item><term>Reference</term><description>GUID → flattened field dictionary of the linked entry (slug, title + all fields).</description></item>
///   <item><term>MultiList</term><description>GUID array → list of flattened field dictionaries.</description></item>
///   <item><term>AssetReference</term><description>Looks up the <see cref="MediaAsset"/> and writes <c>{id, assetUrl, fileName, mimeType, assetType, altText?, width?, height?}</c>.</description></item>
///   <item><term>DateTime</term><description>ISO-8601 string with UTC offset preserved.</description></item>
///   <item><term>All others</term><description>Unchanged.</description></item>
/// </list>
/// </summary>
public sealed class EntryFieldExpander(
    IRepository<Entry, EntryId> entryRepo,
    IRepository<MediaAsset, MediaAssetId> mediaRepo,
    IStorageProvider storage)
{
    /// <summary>
    /// Returns a <see cref="JsonElement"/> where Reference/MultiList fields have been
    /// replaced with the full field content of the linked entries.
    /// </summary>
    public async Task<JsonElement> ExpandAsync(
        Entry entry,
        IReadOnlyList<FieldDefinition> fieldDefs,
        CancellationToken ct = default)
    {
        using var doc = JsonDocument.Parse(entry.FieldsJson);
        if (doc.RootElement.ValueKind != JsonValueKind.Object)
            return doc.RootElement.Clone();

        var fieldDefMap = fieldDefs.ToDictionary(f => f.Handle, StringComparer.OrdinalIgnoreCase);
        var siteId = entry.SiteId;

        // Build an expanded JSON object using a MemoryStream + Utf8JsonWriter.
        using var ms = new System.IO.MemoryStream();
        await using var writer = new Utf8JsonWriter(ms);
        writer.WriteStartObject();

        foreach (var prop in doc.RootElement.EnumerateObject())
        {
            writer.WritePropertyName(prop.Name);

            if (fieldDefMap.TryGetValue(prop.Name, out var fd))
            {
                await WriteExpandedValueAsync(writer, prop.Value, fd, siteId, ct);
            }
            else
            {
                prop.Value.WriteTo(writer);
            }
        }

        writer.WriteEndObject();
        await writer.FlushAsync(ct);

        ms.Position = 0;
        var resultDoc = await JsonDocument.ParseAsync(ms, cancellationToken: ct);
        return resultDoc.RootElement.Clone();
    }

    // ── Private helpers ───────────────────────────────────────────────────

    private async Task WriteExpandedValueAsync(
        Utf8JsonWriter writer,
        JsonElement value,
        FieldDefinition fd,
        SiteId siteId,
        CancellationToken ct)
    {
        switch (fd.FieldType)
        {
            case FieldType.Reference when !fd.IsList:
                await WriteReferenceAsync(writer, value, ct);
                break;

            case FieldType.Reference when fd.IsList:
            case FieldType.MultiList:
                await WriteReferenceListAsync(writer, value, ct);
                break;

            case FieldType.AssetReference:
                await WriteAssetAsync(writer, value, siteId, ct);
                break;

            case FieldType.DateTime:
                WriteDateTime(writer, value);
                break;

            default:
                // All other types (ShortText, RichText, etc.) — pass through unchanged.
                value.WriteTo(writer);
                break;
        }
    }

    /// <summary>
    /// Resolves a single reference GUID to the linked entry's fields.
    /// Falls back to writing a null literal when the entry is not found.
    /// </summary>
    private async Task WriteReferenceAsync(Utf8JsonWriter writer, JsonElement value, CancellationToken ct)
    {
        var entryId = ParseEntryId(value);
        if (entryId is null) { writer.WriteNullValue(); return; }

        var linked = await entryRepo.GetByIdAsync(entryId.Value, ct);
        if (linked is null) { writer.WriteNullValue(); return; }

        WriteEntryFields(writer, linked);
    }

    /// <summary>
    /// Resolves an array of reference GUIDs to a JSON array of entry field objects.
    /// </summary>
    private async Task WriteReferenceListAsync(Utf8JsonWriter writer, JsonElement value, CancellationToken ct)
    {
        writer.WriteStartArray();

        if (value.ValueKind == JsonValueKind.Array)
        {
            foreach (var element in value.EnumerateArray())
            {
                var entryId = ParseEntryId(element);
                if (entryId is null) continue;

                var linked = await entryRepo.GetByIdAsync(entryId.Value, ct);
                if (linked is null) continue;

                WriteEntryFields(writer, linked);
            }
        }

        writer.WriteEndArray();
    }

    /// <summary>
    /// Writes the linked entry as a JSON object containing its slug, title (if present),
    /// and all field values from its <c>FieldsJson</c>.
    /// </summary>
    private static void WriteEntryFields(Utf8JsonWriter writer, Entry linked)
    {
        writer.WriteStartObject();
        writer.WriteString("id", linked.Id.Value.ToString());
        writer.WriteString("slug", linked.Slug.Value);

        try
        {
            using var linkedDoc = JsonDocument.Parse(linked.FieldsJson);
            if (linkedDoc.RootElement.ValueKind == JsonValueKind.Object)
            {
                foreach (var prop in linkedDoc.RootElement.EnumerateObject())
                    prop.WriteTo(writer);
            }
        }
        catch (JsonException) { /* corrupt linked entry — emit only id/slug */ }

        writer.WriteEndObject();
    }

    /// <summary>
    /// Ensures DateTime values are written as ISO-8601 strings preserving offset info.
    /// If the stored value already is a string it is passed through unchanged.
    /// </summary>
    private static void WriteDateTime(Utf8JsonWriter writer, JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.String)
        {
            var raw = value.GetString() ?? string.Empty;
            // Try parsing to DateTimeOffset to normalise timezone representation.
            if (DateTimeOffset.TryParse(raw, null,
                    System.Globalization.DateTimeStyles.RoundtripKind, out var dto))
                writer.WriteStringValue(dto.ToString("O")); // ISO-8601 with offset
            else
                writer.WriteStringValue(raw);
        }
        else
        {
            value.WriteTo(writer);
        }
    }

    private static EntryId? ParseEntryId(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.String)
        {
            var s = element.GetString();
            if (Guid.TryParse(s, out var g))
                return new EntryId(g);
        }
        else if (element.ValueKind == JsonValueKind.Object
                 && element.TryGetProperty("id", out var idProp)
                 && idProp.ValueKind == JsonValueKind.String)
        {
            if (Guid.TryParse(idProp.GetString(), out var g))
                return new EntryId(g);
        }
        return null;
    }

    // ── Asset expansion ───────────────────────────────────────────────────

    /// <summary>
    /// Resolves an AssetReference field value to a rich object containing the asset URL
    /// and metadata. Falls through unchanged if the asset is not found.
    /// </summary>
    private async Task WriteAssetAsync(
        Utf8JsonWriter writer,
        JsonElement value,
        SiteId siteId,
        CancellationToken ct)
    {
        var assetId = ParseAssetId(value);
        if (assetId is null) { value.WriteTo(writer); return; }

        var asset = await mediaRepo.GetByIdAsync(assetId.Value, ct);
        if (asset is null || asset.Status != MediaAssetStatus.Available) { value.WriteTo(writer); return; }

        var url = await storage.GetPublicUrlAsync(asset.StorageKey, ct);
        if (string.IsNullOrEmpty(url))
            url = $"/api/delivery/v1/media/{assetId.Value.Value}/stream?siteId={siteId.Value}";

        writer.WriteStartObject();
        writer.WriteString("id", assetId.Value.Value.ToString());
        writer.WriteString("assetUrl", url);
        writer.WriteString("fileName", asset.Metadata.FileName);
        writer.WriteString("mimeType", asset.Metadata.MimeType);
        writer.WriteString("assetType", DeriveAssetType(asset.Metadata.MimeType));
        if (asset.AltText is not null)
            writer.WriteString("altText", asset.AltText);
        if (asset.Metadata.WidthPx.HasValue)
            writer.WriteNumber("width", asset.Metadata.WidthPx.Value);
        if (asset.Metadata.HeightPx.HasValue)
            writer.WriteNumber("height", asset.Metadata.HeightPx.Value);
        writer.WriteEndObject();
    }

    private static MediaAssetId? ParseAssetId(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.String)
        {
            if (Guid.TryParse(element.GetString(), out var g))
                return new MediaAssetId(g);
        }
        else if (element.ValueKind == JsonValueKind.Object
                 && element.TryGetProperty("id", out var idProp)
                 && idProp.ValueKind == JsonValueKind.String)
        {
            if (Guid.TryParse(idProp.GetString(), out var g))
                return new MediaAssetId(g);
        }
        return null;
    }

    private static string DeriveAssetType(string mimeType) =>
        mimeType.Split('/')[0] switch
        {
            "image" => "image",
            "video" => "video",
            "audio" => "audio",
            _ => mimeType == "application/pdf" ? "document" : "file"
        };
}
