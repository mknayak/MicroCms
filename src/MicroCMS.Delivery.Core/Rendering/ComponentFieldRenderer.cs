using MicroCMS.Application.Common.Interfaces;
using MicroCMS.Application.Features.Delivery.Rendering;
using MicroCMS.Application.Features.Delivery.Services;
using MicroCMS.Domain.Aggregates.Components;
using MicroCMS.Domain.Aggregates.Content;
using MicroCMS.Domain.Aggregates.Media;
using MicroCMS.Domain.Repositories;
using MicroCMS.Domain.Specifications.Components;
using MicroCMS.Domain.Specifications.Content;
using MicroCMS.Shared.Ids;
using Microsoft.Extensions.Logging;

namespace MicroCMS.Delivery.Core.Rendering;

/// <summary>
/// Implements <see cref="IComponentFieldRenderer"/> by fetching the component aggregate
/// and its backing ContentType, then delegating to <see cref="IComponentRenderer"/>
/// to produce a final HTML fragment from the component item's field data.
///
/// Used by <see cref="EntryFieldExpander"/> to render <c>FieldType.Component</c> fields
/// inline during delivery-time expansion.
/// </summary>
internal sealed class ComponentFieldRenderer(
    IRepository<Component, ComponentId> componentRepo,
    IRepository<Entry, EntryId> entryRepo,
    IRepository<ContentType, ContentTypeId> contentTypeRepo,
    IRepository<MediaAsset, MediaAssetId> mediaRepo,
    IStorageProvider storage,
    IComponentRenderer componentRenderer,
    ILogger<ComponentFieldRenderer> logger)
    : IComponentFieldRenderer
{
    public async Task<string?> RenderAsync(
        string componentKey,
        Guid itemEntryId,
        SiteId siteId,
        CancellationToken ct = default)
    {
        // 1. Find the component definition by key within the site.
        var allComponents = await componentRepo.ListAsync(new AllComponentsBySiteSpec(siteId), ct);
        var component = allComponents.FirstOrDefault(c =>
            c.Key.Equals(componentKey, StringComparison.OrdinalIgnoreCase));

        if (component is null)
        {
            logger.LogWarning(
                "ComponentFieldRenderer: component with key '{Key}' not found for site {SiteId}.",
                componentKey, siteId);
            return null;
        }

        // 2. Load the component item entry.
        var item = await entryRepo.GetByIdAsync(new EntryId(itemEntryId), ct);
        if (item is null)
        {
            logger.LogWarning(
                "ComponentFieldRenderer: entry {EntryId} not found for component '{Key}'.",
                itemEntryId, componentKey);
            return null;
        }

        // 3. Resolve the backing ContentType so field definitions are available for expansion.
        ContentType? contentType = null;
        if (component.BackingContentTypeId is { } backingId)
            contentType = await contentTypeRepo.GetByIdAsync(backingId, ct);

        // 4. Expand fields using a renderer-free expander to avoid circular rendering.
        //    (Component fields inside a component item are NOT recursively rendered as HTML;
        //     they fall back to reference-style JSON to prevent unbounded recursion.)
        var expander = new EntryFieldExpander(entryRepo, mediaRepo, storage);

        System.Text.Json.JsonElement fields;
        System.Collections.Generic.IReadOnlyList<
            MicroCMS.Domain.Aggregates.Content.FieldDefinition>? fieldDefs = null;

        if (contentType is not null)
        {
            fieldDefs = contentType.Fields;
            fields = await expander.ExpandAsync(item, fieldDefs, ct);
        }
        else
        {
            fields = System.Text.Json.JsonSerializer
                .Deserialize<System.Text.Json.JsonElement>(item.FieldsJson);
        }

        // 5. Build the DTO and render through the component template engine.
        var dto = new Application.Features.Delivery.Dtos.DeliveryComponentItemDto(
            item.Id.Value,
            component.Id.Value,
            component.Key,
            item.Slug.Value,
            fields);

        return await componentRenderer.RenderAsync(component, dto, fieldDefs, ct);
    }
}
