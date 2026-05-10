using System.Text;
using System.Text.Json;
using MicroCMS.Application.Features.Delivery.Rendering;
using MicroCMS.Domain.Aggregates.Components;
using MicroCMS.Domain.Aggregates.Content;
using MicroCMS.Domain.Aggregates.Pages;
using MicroCMS.Domain.Repositories;
using MicroCMS.Shared.Ids;

namespace MicroCMS.Application.Features.Delivery.Pipeline.Steps;

/// <summary>
/// Step 8 — Zone rendering.
///
/// Iterates component placements in two passes and renders each component
/// into its target zone as an HTML fragment:
///   Pass 1 — SiteTemplate placements (shared base layer, rendered first)
///   Pass 2 — PageTemplate placements (page-specific, appended after site layer)
///
/// The merged zone dictionary is stored in <see cref="PageRenderContext.Zones"/>.
/// </summary>
internal sealed class RenderZonesStep(
    IRepository<Component, ComponentId> compRepo,
    IRepository<Entry, EntryId> entryRepo,
    IComponentRenderingService renderer)
    : IPageRenderStep
{
    public async Task ExecuteAsync(PageRenderContext ctx, Func<Task> next, CancellationToken ct)
    {
        var zoneHtml = new Dictionary<string, StringBuilder>(StringComparer.OrdinalIgnoreCase);

        // Pass 1: SiteTemplate placements (JSON tree format)
        if (ctx.SiteTemplate is not null && ctx.SiteTemplate.PlacementsJson != "[]")
            await AppendPlacementsJsonAsync(ctx.SiteTemplate.PlacementsJson, zoneHtml, ct);

        // Pass 2: PageTemplate placements (legacy navigation list format)
        if (ctx.PageTemplate is not null && ctx.PageTemplate.Placements.Count > 0)
            await AppendLegacyPlacementsAsync(ctx.PageTemplate, zoneHtml, ct);

        ctx.Zones = zoneHtml.ToDictionary(
            kv => kv.Key,
            kv => kv.Value.ToString(),
            StringComparer.OrdinalIgnoreCase);

        await next();
    }

    // ── Legacy placements (PageTemplate) ─────────────────────────────────

    private async Task AppendLegacyPlacementsAsync(
        PageTemplate pageTemplate,
        Dictionary<string, StringBuilder> zones,
        CancellationToken ct)
    {
        foreach (var placement in pageTemplate.Placements.OrderBy(p => p.SortOrder))
        {
            var comp = await compRepo.GetByIdAsync(placement.ComponentId, ct);
            if (comp is null) continue;

            var sb = GetOrAddZone(zones, placement.Zone);

            if (placement.BoundItemId.HasValue)
            {
                var entry = await entryRepo.GetByIdAsync(new EntryId(placement.BoundItemId.Value.Value), ct);
                if (entry is not null)
                    sb.Append(await renderer.RenderComponentAsync(comp, entry, ct));
            }
            else
            {
                sb.Append(await renderer.RenderComponentStaticAsync(comp, ct));
            }
        }
    }

    // ── JSON placements (SiteTemplate) ────────────────────────────────────

    private async Task AppendPlacementsJsonAsync(
        string placementsJson,
        Dictionary<string, StringBuilder> zones,
        CancellationToken ct)
    {
        List<PlacementNode>? nodes;
        try
        {
            nodes = JsonSerializer.Deserialize<List<PlacementNode>>(
                placementsJson,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch (JsonException) { return; }

        if (nodes is null) return;

        await WalkNodesAsync(nodes, zones, ct);
    }

    private async Task WalkNodesAsync(
        IEnumerable<PlacementNode> nodes,
        Dictionary<string, StringBuilder> zones,
        CancellationToken ct)
    {
        foreach (var node in nodes.OrderBy(n => n.SortOrder))
        {
            if (node.Type == "component" && node.ComponentId.HasValue)
                await AppendComponentNodeAsync(node, zones, ct);
            else if (node.Type == "grid-row" && node.Columns is not null)
                foreach (var col in node.Columns)
                    await WalkNodesAsync(col.Placements, zones, ct);
        }
    }

    private async Task AppendComponentNodeAsync(
        PlacementNode node,
        Dictionary<string, StringBuilder> zones,
        CancellationToken ct)
    {
        var comp = await compRepo.GetByIdAsync(new ComponentId(node.ComponentId!.Value), ct);
        if (comp is null) return;

        var sb = GetOrAddZone(zones, node.Zone);

        if (node.BoundItemId.HasValue)
        {
            var entry = await entryRepo.GetByIdAsync(new EntryId(node.BoundItemId.Value), ct);
            if (entry is not null)
                sb.Append(await renderer.RenderComponentAsync(comp, entry, ct));
        }
        else
        {
            sb.Append(await renderer.RenderComponentStaticAsync(comp, ct));
        }
    }

    private static StringBuilder GetOrAddZone(Dictionary<string, StringBuilder> zones, string zone)
    {
        if (zones.TryGetValue(zone, out var existing)) return existing;
        var sb = new StringBuilder();
        zones[zone] = sb;
        return sb;
    }

    // ── Private placement node model ──────────────────────────────────────

    private sealed class PlacementNode
    {
        public string Type { get; init; } = string.Empty;
        public string Zone { get; init; } = string.Empty;
        public int SortOrder { get; init; }
        public Guid? ComponentId { get; init; }
        public Guid? BoundItemId { get; init; }
        public List<GridColumn>? Columns { get; init; }
    }

    private sealed class GridColumn
    {
        public int Span { get; init; }
        public string ZoneName { get; init; } = string.Empty;
        public List<PlacementNode> Placements { get; init; } = [];
    }
}
