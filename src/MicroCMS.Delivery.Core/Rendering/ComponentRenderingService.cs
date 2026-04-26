using MicroCMS.Application.Features.Delivery.Dtos;
using MicroCMS.Application.Features.Delivery.Rendering;
using MicroCMS.Application.Features.Delivery.Handlers;
using MicroCMS.Delivery.Core.Rendering;
using MicroCMS.Domain.Aggregates.Components;

namespace MicroCMS.Delivery.Core.Rendering;

/// <summary>
/// Bridges the Application-layer <see cref="IComponentRenderingService"/> to the
/// concrete <see cref="IComponentRenderer"/> and <see cref="ILayoutRenderer"/>
/// engines that live in <c>MicroCMS.Delivery.Core</c>.
/// </summary>
internal sealed class ComponentRenderingService(
    IComponentRenderer componentRenderer,
    ILayoutRenderer layoutRenderer)
    : IComponentRenderingService
{
    public Task<string> RenderComponentAsync(
        Component component,
        ComponentItem item,
        CancellationToken cancellationToken = default)
    {
        var dto = new DeliveryComponentItemDto(
     item.Id.Value,
         item.ComponentId.Value,
     component.Key,
  item.Title,
     System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(item.FieldsJson));

        return componentRenderer.RenderAsync(component, dto, cancellationToken);
    }

    public Task<string> RenderLayoutAsync(
   Layout layout,
      IReadOnlyDictionary<string, string> zones,
   string? seoTitle       = null,
        string? seoDescription = null,
        string? seoOgImage     = null,
   CancellationToken cancellationToken = default) =>
 layoutRenderer.RenderAsync(layout, zones, seoTitle, seoDescription, seoOgImage, cancellationToken);
}
