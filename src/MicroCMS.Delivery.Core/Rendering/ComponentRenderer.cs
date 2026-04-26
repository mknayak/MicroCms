using HandlebarsDotNet;
using MicroCMS.Application.Features.Delivery.Dtos;
using MicroCMS.Domain.Aggregates.Components;
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
///   <item><term>WebComponent</term><description>Emits a hydration hint comment.</description></item>
///   <item><term>RazorPartial</term><description>Emits a hydration hint comment. Rendering must be done by an MVC host via <c>Html.PartialAsync(component.Key)</c>.</description></item>
/// </list>
/// </summary>
public interface IComponentRenderer
{
    Task<string> RenderAsync(
        Component component,
     DeliveryComponentItemDto item,
        CancellationToken cancellationToken = default);
}

internal sealed class ComponentRenderer(ILogger<ComponentRenderer> logger) : IComponentRenderer
{
    public Task<string> RenderAsync(
Component component,
        DeliveryComponentItemDto item,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(component.TemplateContent))
            return Task.FromResult(string.Empty);

        var html = component.TemplateType switch
        {
            RenderingTemplateType.Handlebars => RenderHandlebars(component, item),
            _ => RenderFallbackComment(component, item),
        };

        return Task.FromResult(html);
    }

    // ── Handlebars ────────────────────────────────────────────────────────

    private string RenderHandlebars(Component component, DeliveryComponentItemDto item)
    {
        try
        {
            var template = Handlebars.Compile(component.TemplateContent!);
            return template(BuildDataDictionary(item));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Handlebars render failed for component {Key}", component.Key);
            return $"<!-- render-error component:{component.Key} -->";
        }
    }

    // ── Fallback ──────────────────────────────────────────────────────────

    private static string RenderFallbackComment(Component component, DeliveryComponentItemDto item) =>
        $"<!-- component:{component.Key} id:{item.Id} type:{component.TemplateType} -->";

    // ── Helpers ───────────────────────────────────────────────────────────

    /// <summary>
    /// Flattens the JSON fields bag into a string-keyed dictionary so Handlebars
    /// can bind values by name, e.g. <c>{{heading}}</c>.
    /// </summary>
    private static Dictionary<string, object?> BuildDataDictionary(DeliveryComponentItemDto item)
    {
        var dict = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

        if (item.Fields is JsonElement je && je.ValueKind == JsonValueKind.Object)
        {
            foreach (var prop in je.EnumerateObject())
            {
                dict[prop.Name] = prop.Value.ValueKind switch
                {
                    JsonValueKind.String => prop.Value.GetString(),
                    JsonValueKind.Number => prop.Value.TryGetInt64(out var l)
                          ? (object)l
                          : prop.Value.GetDouble(),
                    JsonValueKind.True => (object)true,
                    JsonValueKind.False => false,
                    JsonValueKind.Null => null,
                    _ => prop.Value.ToString(),
                };
            }
        }

        return dict;
    }
}
