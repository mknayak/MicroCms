using System.Text.Json;
using MicroCMS.Application.Features.Components.Commands;
using MicroCMS.Application.Features.Components.Dtos;
using MicroCMS.Application.Features.Components.Queries;
using MicroCMS.Shared.Primitives;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MicroCMS.Api.Controllers;

/// <summary>Component library — schema definitions and item instances.</summary>
[Authorize]
public sealed class ComponentsController : ApiControllerBase
{
    // ── Component definitions ────────────────────────────────────────────

    [HttpGet]
    [ProducesResponseType(typeof(PagedList<ComponentListItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(
      [FromQuery] Guid siteId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
  CancellationToken cancellationToken = default)
    {
   var result = await Sender.Send(new ListComponentsQuery(siteId, page, pageSize), cancellationToken);
      return OkOrProblem(result);
    }

    [HttpGet("{id:guid}")]
  [ProducesResponseType(typeof(ComponentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await Sender.Send(new GetComponentQuery(id), cancellationToken);
        return OkOrProblem(result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(ComponentDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Create(
        [FromBody] CreateComponentCommand command,
        CancellationToken cancellationToken = default)
    {
        var result = await Sender.Send(command, cancellationToken);
     return CreatedOrProblem(result, nameof(Get), new { id = result.IsSuccess ? result.Value.Id : Guid.Empty });
    }

[HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ComponentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateComponentRequest request,
      CancellationToken cancellationToken = default)
    {
        var result = await Sender.Send(
 new UpdateComponentCommand(id, request.Name, request.Description,
      request.Category, request.Zones, request.Fields),
            cancellationToken);
        return OkOrProblem(result);
    }

    [HttpDelete("{id:guid}")]
 [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await Sender.Send(new DeleteComponentCommand(id), cancellationToken);
        return NoContentOrProblem(result);
    }

    [HttpPut("{id:guid}/template")]
    [ProducesResponseType(typeof(ComponentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> UpdateTemplate(
        Guid id,
        [FromBody] UpdateComponentTemplateRequest request,
 CancellationToken cancellationToken = default)
    {
        var result = await Sender.Send(
   new UpdateComponentTemplateCommand(id, request.TemplateType, request.TemplateContent),
          cancellationToken);
        return OkOrProblem(result);
    }

    // ── Component items (instances) ──────────────────────────────────────

  [HttpGet("{id:guid}/items")]
    [ProducesResponseType(typeof(PagedList<ComponentItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListItems(
        Guid id,
    [FromQuery] string? status = null,
     [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
    var result = await Sender.Send(new ListComponentItemsQuery(id, status, page, pageSize), cancellationToken);
        return OkOrProblem(result);
    }

    [HttpGet("{id:guid}/items/{itemId:guid}")]
    [ProducesResponseType(typeof(ComponentItemDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetItem(
     Guid id, Guid itemId, CancellationToken cancellationToken = default)
 {
   var result = await Sender.Send(new GetComponentItemQuery(id, itemId), cancellationToken);
        return OkOrProblem(result);
    }

    [HttpPost("{id:guid}/items")]
    [ProducesResponseType(typeof(ComponentItemDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateItem(
        Guid id,
        [FromBody] CreateComponentItemRequest request,
   CancellationToken cancellationToken = default)
    {
        var result = await Sender.Send(
 new CreateComponentItemCommand(id, request.Title, request.FieldsJson.GetRawText()),
   cancellationToken);
        return CreatedOrProblem(result, nameof(GetItem),
 new { id, itemId = result.IsSuccess ? result.Value.Id : Guid.Empty });
    }

    [HttpPut("{id:guid}/items/{itemId:guid}")]
    [ProducesResponseType(typeof(ComponentItemDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateItem(
        Guid id,
  Guid itemId,
      [FromBody] UpdateComponentItemRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await Sender.Send(
            new UpdateComponentItemCommand(id, itemId, request.Title, request.FieldsJson.GetRawText()),
        cancellationToken);
        return OkOrProblem(result);
    }

    [HttpPost("{id:guid}/items/{itemId:guid}/publish")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> PublishItem(
        Guid id, Guid itemId, CancellationToken cancellationToken = default)
    {
    var result = await Sender.Send(new PublishComponentItemCommand(id, itemId), cancellationToken);
 return NoContentOrProblem(result);
    }

    [HttpPost("{id:guid}/items/{itemId:guid}/archive")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ArchiveItem(
      Guid id, Guid itemId, CancellationToken cancellationToken = default)
    {
     var result = await Sender.Send(new ArchiveComponentItemCommand(id, itemId), cancellationToken);
        return NoContentOrProblem(result);
    }

    [HttpDelete("{id:guid}/items/{itemId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteItem(
    Guid id, Guid itemId, CancellationToken cancellationToken = default)
    {
        var result = await Sender.Send(new DeleteComponentItemCommand(id, itemId), cancellationToken);
   return NoContentOrProblem(result);
    }
}

// ── Request types ──────────────────────────────────────────────────────────────

public sealed record UpdateComponentRequest(
    string Name,
    string? Description,
    string Category,
    IReadOnlyList<string> Zones,
    IReadOnlyList<Application.Features.Components.Commands.ComponentFieldInput> Fields);

public sealed record UpdateComponentTemplateRequest(
    string TemplateType,
    string? TemplateContent);

public sealed record CreateComponentItemRequest(string Title, JsonElement FieldsJson);

public sealed record UpdateComponentItemRequest(string Title, JsonElement FieldsJson);
