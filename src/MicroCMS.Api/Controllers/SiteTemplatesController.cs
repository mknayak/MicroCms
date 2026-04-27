using MicroCMS.Application.Features.SiteTemplates.Commands;
using MicroCMS.Application.Features.SiteTemplates.Dtos;
using MicroCMS.Application.Features.SiteTemplates.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MicroCMS.Api.Controllers;

/// <summary>
/// Manages reusable Site Templates — named sets of component placements linked to a Layout.
/// Pages that reference a template inherit its placements automatically.
///
/// Route: /api/v1/site-templates
/// </summary>
[Authorize]
public sealed class SiteTemplatesController : ApiControllerBase
{
    /// <summary>Lists all site templates for a given site.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<SiteTemplateListItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(
        [FromQuery] Guid siteId,
        CancellationToken ct = default) =>
OkOrProblem(await Sender.Send(new ListSiteTemplatesQuery(siteId), ct));

    /// <summary>Gets a single site template by ID.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(SiteTemplateDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct = default) =>
      OkOrProblem(await Sender.Send(new GetSiteTemplateQuery(id), ct));

/// <summary>Creates a new site template linked to a layout.</summary>
    [HttpPost]
 [ProducesResponseType(typeof(SiteTemplateDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Create(
        [FromBody] CreateSiteTemplateRequest request,
        CancellationToken ct = default)
    {
  var result = await Sender.Send(
   new CreateSiteTemplateCommand(request.SiteId, request.LayoutId, request.Name, request.Description), ct);
        return CreatedOrProblem(result, nameof(Get), new { id = result.IsSuccess ? result.Value.Id : Guid.Empty });
    }

    /// <summary>Updates a template's name, description and linked layout.</summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(SiteTemplateDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateSiteTemplateRequest request,
     CancellationToken ct = default) =>
        OkOrProblem(await Sender.Send(
            new UpdateSiteTemplateCommand(id, request.LayoutId, request.Name, request.Description), ct));

    /// <summary>
    /// Replaces the full component-placement tree for a template.
    /// Accepts the same JSON format used by the Page Designer canvas.
    /// </summary>
    [HttpPut("{id:guid}/placements")]
    [ProducesResponseType(typeof(SiteTemplateDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SavePlacements(
    Guid id,
      [FromBody] SaveSiteTemplatePlacementsRequest request,
        CancellationToken ct = default) =>
        OkOrProblem(await Sender.Send(
    new SaveSiteTemplatePlacementsCommand(id, request.PlacementsJson), ct));

    /// <summary>Deletes a site template. Pages that reference it lose their inherited placements.</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
public async Task<IActionResult> Delete(Guid id, CancellationToken ct = default) =>
     NoContentOrProblem(await Sender.Send(new DeleteSiteTemplateCommand(id), ct));
}

// ── Request bodies ────────────────────────────────────────────────────────────

public sealed record CreateSiteTemplateRequest(
    Guid SiteId,
  Guid LayoutId,
    string Name,
    string? Description);

public sealed record UpdateSiteTemplateRequest(
    Guid LayoutId,
    string Name,
    string? Description);

public sealed record SaveSiteTemplatePlacementsRequest(string PlacementsJson);
