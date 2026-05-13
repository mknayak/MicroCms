using MicroCMS.Application.Features.Delivery.Dtos;
using MicroCMS.Application.Features.Delivery.Rendering;
using MicroCMS.Application.Features.Delivery.Handlers;
using MicroCMS.Application.Features.Delivery.Services;
using MicroCMS.Delivery.Core.Rendering;
using MicroCMS.Domain.Aggregates.Components;
using MicroCMS.Domain.Aggregates.Content;

namespace MicroCMS.Delivery.Core.Rendering;

/// <summary>
/// Bridges the Application-layer <see cref="IComponentRenderingService"/> to the
/// concrete <see cref="IComponentRenderer"/> and <see cref="ILayoutRenderer"/>
/// engines that live in <c>MicroCMS.Delivery.Core</c>.
/// </summary>
internal sealed class ComponentRenderingService(
    IComponentRenderer componentRenderer,
    ILayoutRenderer layoutRenderer,
    EntryFieldExpander fieldExpander)
    : IComponentRenderingService
{
    public async Task<string> RenderComponentAsync(
        Component component,
        Entry item,
        ContentType? contentType = null,
        CancellationToken cancellationToken = default)
    {
        System.Text.Json.JsonElement fields;
        System.Collections.Generic.IReadOnlyList<Domain.Aggregates.Content.FieldDefinition>? fieldDefs = null;

        if (contentType is not null)
        {
            // Expand Reference/MultiList fields and capture field definitions for type-aware rendering.
            fieldDefs = contentType.Fields;
            fields = await fieldExpander.ExpandAsync(item, fieldDefs, cancellationToken);
        }
        else
        {
            fields = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(item.FieldsJson);
        }

        var dto = new DeliveryComponentItemDto(
            item.Id.Value,
            component.Id.Value,
            component.Key,
            item.Slug.Value,
            fields);

        return await componentRenderer.RenderAsync(component, dto, fieldDefs, cancellationToken);
    }

    public Task<string> RenderComponentStaticAsync(
        Component component,
        CancellationToken cancellationToken = default)
    {
        var dto = new DeliveryComponentItemDto(
            System.Guid.Empty,
            component.Id.Value,
            component.Key,
            string.Empty,
            System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>("{}"));

        return componentRenderer.RenderAsync(component, dto, null, cancellationToken);
    }

    public Task<string> RenderLayoutAsync(
        Layout layout,
        IReadOnlyDictionary<string, string> zones,
        RenderContext renderContext,
        string? seoTitle       = null,
        string? seoDescription = null,
        string? seoOgImage     = null,
        CancellationToken cancellationToken = default) =>
        layoutRenderer.RenderAsync(layout, zones, renderContext, seoTitle, seoDescription, seoOgImage, cancellationToken);
}
