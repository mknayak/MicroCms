using System.Text.RegularExpressions;
using Scriban;
using Scriban.Runtime;
using MicroCMS.Application.Features.Delivery.Dtos;
using MicroCMS.Domain.Aggregates.Components;
using MicroCMS.Domain.Aggregates.Content;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace MicroCMS.Delivery.Core.Rendering;

/// <summary>
/// Renders a component item to HTML using the parent component's
/// <see cref="RenderingTemplateType"/> and <c>TemplateContent</c>.
///
/// Rendering behaviour by type:
/// <list type="table">
///   <item><term>Handlebars</term><description>Rendered server-side via Handlebars.Net. Fields JSON is flattened to named tokens.</description></item>
///   <item><term>React</term><description>Emits <c>&lt;!-- component:key id:... type:React --&gt;</c> for client-side hydration.</description></item>
///   <item><term>WebComponent</term><description>Renders <c>TemplateContent</c> with <c>{{fieldName}}</c> token substitution, allowing attribute bindings on custom elements.</description></item>
///   <item><term>Html</term><description>Returns <c>TemplateContent</c> as-is after substituting <c>{{fieldName}}</c> tokens with item field values.</description></item>
///   <item><term>RazorPartial</term><description>Emits a hydration hint comment. Rendering must be done by an MVC host via <c>Html.PartialAsync(component.Key)</c>.</description></item>
/// </list>
/// </summary>
public interface IComponentRenderer
{
    Task<string> RenderAsync(
        Component component,
        DeliveryComponentItemDto item,
        IReadOnlyList<FieldDefinition>? fieldDefs = null,
        CancellationToken cancellationToken = default);
}

internal sealed class ComponentRenderer(ILogger<ComponentRenderer> logger) : IComponentRenderer
{
    public Task<string> RenderAsync(
        Component component,
        DeliveryComponentItemDto item,
        IReadOnlyList<FieldDefinition>? fieldDefs = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(component.TemplateContent))
            return Task.FromResult(string.Empty);

        var html = component.TemplateType switch
        {
            RenderingTemplateType.Scriban => RenderScriban(component, item),
            RenderingTemplateType.Html => RenderHtml(component, item),
            RenderingTemplateType.WebComponent => RenderHtml(component, item),
            _ => RenderFallbackComment(component, item),
        };

        return Task.FromResult(html);
    }

    // ── Scriban ───────────────────────────────────────────────────────────

    private string RenderScriban(Component component, DeliveryComponentItemDto item)
    {
        try
        {
            var processed = ScribanHelpers.Stash(component.TemplateContent!, out var stash);
            var template = Template.Parse(processed);
            if (template.HasErrors)
            {
                logger.LogError("Scriban parse errors for component {Key}: {Errors}",
                    component.Key, string.Join("; ", template.Messages));
                return $"<!-- render-error component:{component.Key} -->";
            }

            var scriptObj = new ScriptObject();
            foreach (var (key, value) in BuildDataDictionary(item))
                scriptObj.Add(key, value);

            var ctx = new TemplateContext { StrictVariables = false };
            ctx.PushGlobal(scriptObj);

            var result = template.Render(ctx);
            return ScribanHelpers.Restore(result, stash);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Scriban render failed for component {Key}", component.Key);
            return $"<!-- render-error component:{component.Key} -->";
        }
    }

    // ── HTML ──────────────────────────────────────────────────────────────

    private string RenderHtml(Component component, DeliveryComponentItemDto item)
    {
        try
        {
            var data = BuildDataDictionary(item);
            return Regex.Replace(
                component.TemplateContent!,
                @"\{\{([a-zA-Z][a-zA-Z0-9_]*)\}\}",
                m => data.TryGetValue(m.Groups[1].Value, out var val) ? val?.ToString() ?? string.Empty : m.Value);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "HTML render failed for component {Key}", component.Key);
            return $"<!-- render-error component:{component.Key} -->";
        }
    }

    // ── Fallback ──────────────────────────────────────────────────────────

    private static string RenderFallbackComment(Component component, DeliveryComponentItemDto item) =>
        $"<!-- component:{component.Key} id:{item.Id} type:{component.TemplateType} -->";

    // ── Helpers ───────────────────────────────────────────────────────────

    /// <summary>
    /// Converts the entry's field JSON into a CLR object graph for Scriban:
    /// <list type="bullet">
    ///   <item>JSON strings → <c>string</c></item>
    ///   <item>JSON arrays  → <c>List&lt;object?&gt;</c> — enables <c>{{ for item in items }}</c> loop syntax</item>
    ///   <item>JSON objects → <c>Dictionary&lt;string, object?&gt;</c> — enables <c>{{ asset.url }}</c> dot-access</item>
    /// </list>
    /// All output is raw (no HTML encoding). Use <c>{{ field | html.escape }}</c> to encode.
    /// </summary>
    private static Dictionary<string, object?> BuildDataDictionary(DeliveryComponentItemDto item)
    {
        var dict = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

        if (item.Fields is not JsonElement je || je.ValueKind != JsonValueKind.Object)
            return dict;

        foreach (var prop in je.EnumerateObject())
            dict[prop.Name] = ConvertJsonElement(prop.Value);

        return dict;
    }

    /// <summary>
    /// Recursively converts a <see cref="JsonElement"/> to a CLR value suitable for Scriban.
    /// </summary>
    private static object? ConvertJsonElement(JsonElement el)
    {
        return el.ValueKind switch
        {
            JsonValueKind.String => el.GetString(),
            JsonValueKind.Number => el.TryGetInt64(out var l) ? (object?)l : el.GetDouble(),
            JsonValueKind.True   => (object?)true,
            JsonValueKind.False  => false,
            JsonValueKind.Null   => null,
            JsonValueKind.Array  => el.EnumerateArray()
                                      .Select(e => ConvertJsonElement(e))
                                      .ToList(),
            JsonValueKind.Object => el.EnumerateObject()
                                      .ToDictionary(
                                          p => p.Name,
                                          p => ConvertJsonElement(p.Value),
                                          StringComparer.OrdinalIgnoreCase),
            _                    => el.ToString(),
        };
    }
}
