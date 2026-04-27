using MicroCMS.Application.Features.Layouts.Commands;
using MicroCMS.Application.Features.Layouts.Dtos;
using MicroCMS.Application.Features.Layouts.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MicroCMS.Api.Controllers;

/// <summary>
/// Manages Layouts — the master HTML shells that wrap rendered pages.
///
/// Layouts are global per site and can be assigned to individual pages or
/// set as the site-wide default.
/// </summary>
[Authorize]
public sealed class LayoutsController : ApiControllerBase
{
    /// <summary>Lists all layouts for a site.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<LayoutListItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(
        [FromQuery] Guid siteId,
        CancellationToken ct = default) =>
        OkOrProblem(await Sender.Send(new ListLayoutsQuery(siteId), ct));

    /// <summary>Gets a single layout by ID.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(LayoutDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct = default) =>
        OkOrProblem(await Sender.Send(new GetLayoutQuery(id), ct));

    /// <summary>
 /// Creates a new layout.
///
    /// <b>TemplateType</b> values: <c>Handlebars</c> (default), <c>Html</c>.
    ///
    /// <b>ShellTemplate</b> zone placeholders:
    /// <ul>
    ///   <li><c>{{zone:hero-zone}}</c> — replaced with rendered component HTML for that zone.</li>
    ///   <li><c>{{seo:title}}</c>, <c>{{seo:description}}</c>, <c>{{seo:ogImage}}</c> — SEO tokens.</li>
    /// </ul>
    /// For Handlebars layouts, zones are also available as <c>{{{zone_hero_zone}}}</c>
  /// (hyphens replaced with underscores, triple-stash for unescaped HTML).
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(LayoutDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create(
        [FromBody] CreateLayoutCommand command,
  CancellationToken ct = default)
    {
        var result = await Sender.Send(command, ct);
        return CreatedOrProblem(result, nameof(Get), new { id = result.IsSuccess ? result.Value.Id : Guid.Empty });
    }

    /// <summary>Updates a layout's name, template type, and shell template content.</summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(LayoutDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Update(
        Guid id,
      [FromBody] UpdateLayoutRequest request,
      CancellationToken ct = default) =>
        OkOrProblem(await Sender.Send(new UpdateLayoutCommand(id, request.Name, request.TemplateType), ct));

    /// <summary>
    /// Replaces the zone tree for a layout.
    /// The shell template is auto-regenerated from the new zone structure.
    /// </summary>
    [HttpPut("{id:guid}/zones")]
    [ProducesResponseType(typeof(LayoutDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateZones(
        Guid id, [FromBody] UpdateLayoutZonesRequest request, CancellationToken ct = default) =>
        OkOrProblem(await Sender.Send(new UpdateLayoutZonesCommand(id, request.Zones), ct));

    /// <summary>Replaces the default component placements for a layout.</summary>
    [HttpPut("{id:guid}/default-placements")]
    [ProducesResponseType(typeof(LayoutDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateDefaultPlacements(
    Guid id, [FromBody] UpdateDefaultPlacementsRequest request, CancellationToken ct = default) =>
        OkOrProblem(await Sender.Send(new UpdateLayoutDefaultPlacementsCommand(id, request.Placements), ct));

    /// <summary>Sets this layout as the site default. Clears IsDefault on any previous default.</summary>
    [HttpPost("{id:guid}/set-default")]
    [ProducesResponseType(typeof(LayoutDto), StatusCodes.Status200OK)]
  public async Task<IActionResult> SetDefault(Guid id, [FromQuery] Guid siteId, CancellationToken ct = default) =>
        OkOrProblem(await Sender.Send(new SetDefaultLayoutCommand(siteId, id), ct));

    /// <summary>Deletes a layout. Pages that reference it will fall back to the site default.</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct = default) =>
        NoContentOrProblem(await Sender.Send(new DeleteLayoutCommand(id), ct));
}

// ── Request bodies ────────────────────────────────────────────────────────────
public sealed record UpdateLayoutRequest(string Name, string TemplateType);
public sealed record UpdateLayoutZonesRequest(IReadOnlyList<LayoutZoneNodeDto> Zones);
public sealed record UpdateDefaultPlacementsRequest(IReadOnlyList<LayoutDefaultPlacementDto> Placements);
