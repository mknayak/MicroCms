using System.Text.Json;
using MicroCMS.Domain.Aggregates.Content;
using MicroCMS.Domain.Repositories;
using MicroCMS.Shared.Ids;

namespace MicroCMS.Application.Features.Delivery.Pipeline.Steps;

/// <summary>
/// Step 7 — Page field resolution.
///
/// Loads the entry linked to the page (when present) and flattens its
/// <c>FieldsJson</c> into <see cref="PageRenderContext.PageFields"/>.
/// This makes <c>{{page:fieldName}}</c> tokens resolve during layout rendering.
/// </summary>
internal sealed class ResolvePageFieldsStep(IRepository<Entry, EntryId> entryRepo) : IPageRenderStep
{
    public async Task ExecuteAsync(PageRenderContext ctx, Func<Task> next, CancellationToken ct)
    {
        if (ctx.Page!.LinkedEntryId is not null)
        {
            var entry = await entryRepo.GetByIdAsync(ctx.Page.LinkedEntryId.Value, ct);
            ctx.LinkedEntry = entry;
            if (entry is not null)
                ctx.PageFields = FlattenFieldsJson(entry.FieldsJson);
        }

        await next();
    }

    private static IReadOnlyDictionary<string, string> FlattenFieldsJson(string fieldsJson)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        try
        {
            using var doc = JsonDocument.Parse(fieldsJson);
            if (doc.RootElement.ValueKind != JsonValueKind.Object) return result;
            foreach (var prop in doc.RootElement.EnumerateObject())
            {
                result[prop.Name] = prop.Value.ValueKind switch
                {
                    JsonValueKind.String => prop.Value.GetString() ?? string.Empty,
                    JsonValueKind.Null   => string.Empty,
                    JsonValueKind.True   => "true",
                    JsonValueKind.False  => "false",
                    _                    => prop.Value.ToString(),
                };
            }
        }
        catch (JsonException) { }

        return result;
    }
}
