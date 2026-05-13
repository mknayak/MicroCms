using System.Text.Json;
using MicroCMS.Domain.Aggregates.Content;
using MicroCMS.Domain.Enums;
using MicroCMS.Domain.Repositories;
using MicroCMS.Shared.Ids;

namespace MicroCMS.Application.Features.Delivery.Pipeline.Steps;

/// <summary>
/// Step 7 — Page field resolution.
///
/// Loads the entry linked to the page (when present) and flattens its
/// <c>FieldsJson</c> into <see cref="PageRenderContext.PageFields"/>.
/// This makes <c>{{page:fieldName}}</c> tokens resolve during layout rendering.
///
/// Reference fields are expanded with dot-notation so that
/// <c>{{page:linkedarticle.title}}</c> resolves to the linked entry's title field.
/// </summary>
internal sealed class ResolvePageFieldsStep(
    IRepository<Entry, EntryId> entryRepo,
    IRepository<ContentType, ContentTypeId> contentTypeRepo)
    : IPageRenderStep
{
    public async Task ExecuteAsync(PageRenderContext ctx, Func<Task> next, CancellationToken ct)
    {
        if (ctx.Page!.LinkedEntryId is not null)
        {
            var entry = await entryRepo.GetByIdAsync(ctx.Page.LinkedEntryId.Value, ct);
            ctx.LinkedEntry = entry;
            if (entry is not null)
            {
                var contentType = await contentTypeRepo.GetByIdAsync(entry.ContentTypeId, ct);
                ctx.PageFields = await FlattenFieldsJsonAsync(entry.FieldsJson, contentType, entryRepo, ct);
            }
        }

        await next();
    }

    private static async Task<IReadOnlyDictionary<string, string>> FlattenFieldsJsonAsync(
        string fieldsJson,
        ContentType? contentType,
        IRepository<Entry, EntryId> entryRepo,
        CancellationToken ct)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        try
        {
            using var doc = JsonDocument.Parse(fieldsJson);
            if (doc.RootElement.ValueKind != JsonValueKind.Object) return result;

            var fieldDefMap = contentType?.Fields
                .ToDictionary(f => f.Handle, StringComparer.OrdinalIgnoreCase);

            foreach (var prop in doc.RootElement.EnumerateObject())
                await FlattenPropertyAsync(result, prop, fieldDefMap, entryRepo, ct);
        }
        catch (JsonException) { }

        return result;
    }

    private static async Task FlattenPropertyAsync(
        Dictionary<string, string> result,
        JsonProperty prop,
        Dictionary<string, FieldDefinition>? fieldDefMap,
        IRepository<Entry, EntryId> entryRepo,
        CancellationToken ct)
    {
        FieldDefinition? fd = null;
        fieldDefMap?.TryGetValue(prop.Name, out fd);

        switch (GetFieldCategory(fd))
        {
            case FieldCategory.Reference:
                await ExpandReferenceAsync(result, prop.Name, prop.Value, entryRepo, ct);
                break;
            case FieldCategory.ReferenceList:
                result[prop.Name] = prop.Value.ToString();
                break;
            case FieldCategory.DateTime:
                result[prop.Name] = NormaliseDateTimeToken(prop.Value);
                break;
            default:
                result[prop.Name] = ScalarToString(prop.Value);
                break;
        }
    }

    private enum FieldCategory { Scalar, DateTime, Reference, ReferenceList }

    private static FieldCategory GetFieldCategory(FieldDefinition? fd)
    {
        if (fd is null) return FieldCategory.Scalar;
        if (fd.FieldType == FieldType.DateTime) return FieldCategory.DateTime;
        if (fd.FieldType == FieldType.MultiList) return FieldCategory.ReferenceList;
        if (fd.FieldType == FieldType.Reference) return fd.IsList ? FieldCategory.ReferenceList : FieldCategory.Reference;
        return FieldCategory.Scalar;
    }

    private static string ScalarToString(JsonElement el) => el.ValueKind switch
    {
        JsonValueKind.String => el.GetString() ?? string.Empty,
        JsonValueKind.Null   => string.Empty,
        JsonValueKind.True   => "true",
        JsonValueKind.False  => "false",
        _                    => el.ToString(),
    };

    /// <summary>
    /// Adds dot-notation entries for a reference field, e.g.
    /// <c>linkedarticle.title</c>, <c>linkedarticle.slug</c>, etc.
    /// Also stores the raw GUID under <c>linkedarticle</c> for backwards compatibility.
    /// </summary>
    private static async Task ExpandReferenceAsync(
        Dictionary<string, string> result,
        string fieldName,
        JsonElement value,
        IRepository<Entry, EntryId> entryRepo,
        CancellationToken ct)
    {
        // Store raw id/guid as fallback.
        result[fieldName] = value.ValueKind == JsonValueKind.String
            ? value.GetString() ?? string.Empty
            : value.ToString();

        if (value.ValueKind != JsonValueKind.String) return;
        if (!Guid.TryParse(value.GetString(), out var guid)) return;

        var linked = await entryRepo.GetByIdAsync(new EntryId(guid), ct);
        if (linked is null) return;

        result[$"{fieldName}.slug"] = linked.Slug.Value;
        WriteDotNotationFields(result, fieldName, linked.FieldsJson);
    }

    private static void WriteDotNotationFields(
        Dictionary<string, string> result,
        string prefix,
        string fieldsJson)
    {
        try
        {
            using var linkedDoc = JsonDocument.Parse(fieldsJson);
            if (linkedDoc.RootElement.ValueKind != JsonValueKind.Object) return;

            foreach (var prop in linkedDoc.RootElement.EnumerateObject())
                result[$"{prefix}.{prop.Name}"] = ScalarToString(prop.Value);
        }
        catch (JsonException) { }
    }

    private static string NormaliseDateTimeToken(JsonElement value)
    {
        if (value.ValueKind != JsonValueKind.String) return value.ToString();

        var raw = value.GetString() ?? string.Empty;
        return DateTimeOffset.TryParse(raw, null,
            System.Globalization.DateTimeStyles.RoundtripKind, out var dto)
            ? dto.ToString("O")
            : raw;
    }
}
