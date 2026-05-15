using System.Text.RegularExpressions;
using HandlebarsDotNet;
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
    // Matches {{namespace:key}} tokens that belong to the layout token pipeline,
    // distinguished from plain Handlebars field bindings by the colon separator.
    private static readonly Regex NamespaceTokenPattern = new(
        @"\{\{([a-zA-Z][a-zA-Z0-9_-]*(?::[a-zA-Z0-9_\-\.]+)+)\}\}",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    // Register a global {{html value}} helper once so RichText templates can output
    // unescaped HTML without requiring triple-brace {{{syntax}}}.
    // Triple-brace syntax is still fully supported and is the recommended way.
    static ComponentRenderer()
    {
        Handlebars.RegisterHelper("html", (writer, _, arguments) =>
        {
            if (arguments.Length > 0)
                writer.WriteSafeString(arguments[0]?.ToString() ?? string.Empty);
        });

    }
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
            RenderingTemplateType.Handlebars => RenderHandlebars(component, item, fieldDefs),
            RenderingTemplateType.Html => RenderHtml(component, item),
            RenderingTemplateType.WebComponent => RenderHtml(component, item),
            _ => RenderFallbackComment(component, item),
        };

        return Task.FromResult(html);
    }

    // ── Handlebars ────────────────────────────────────────────────────────

    private string RenderHandlebars(Component component, DeliveryComponentItemDto item, IReadOnlyList<FieldDefinition>? fieldDefs)
    {
        try
        {
            var escaped = EscapeNamespaceTokens(component.TemplateContent!);
            var template = Handlebars.Compile(escaped);
            return template(BuildDataDictionary(item));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Handlebars render failed for component {Key}", component.Key);
            return $"<!-- render-error component:{component.Key} -->";
        }
    }

    /// <summary>
    /// Replaces <c>{{ns:key}}</c> with <c>\{{ns:key}}</c> so Handlebars outputs
    /// them as literal text instead of attempting (and silently failing) a field lookup.
    /// </summary>
    private static string EscapeNamespaceTokens(string template) =>
        NamespaceTokenPattern.Replace(template, @"\{{$1}}");

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
    /// Converts the entry's field JSON into a rich CLR object graph for Handlebars:
    /// <list type="bullet">
    ///   <item>JSON strings → <c>string</c></item>
    ///   <item>JSON arrays  → <c>List&lt;object?&gt;</c> — enables <c>{{#each fieldName}}</c> loop syntax</item>
    ///   <item>JSON objects → <c>Dictionary&lt;string, object?&gt;</c> — enables <c>{{asset.url}}</c>, <c>{{ref.title}}</c> dot-access</item>
    /// </list>
    /// RichText/Markdown fields: use <c>{{{fieldName}}}</c> (triple braces) in the template
    /// to output raw HTML, or the registered <c>{{html fieldName}}</c> helper.
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
    /// Recursively converts a <see cref="JsonElement"/> to a CLR value suitable for Handlebars.
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
