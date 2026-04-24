using MicroCMS.Application.Features.ContentTypes.Commands;
using MicroCMS.Application.Features.ContentTypes.Dtos;
using MicroCMS.Application.Features.ContentTypes.Queries;
using MicroCMS.Shared.Primitives;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MicroCMS.Api.Controllers;

/// <summary>Content type schema definition — create, manage fields, publish, archive.</summary>
[Authorize]
public sealed class ContentTypesController : ApiControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(PagedList<ContentTypeListItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(
        [FromQuery] Guid? siteId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await Sender.Send(new ListContentTypesQuery(siteId, page, pageSize), cancellationToken);
        return OkOrProblem(result);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ContentTypeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await Sender.Send(new GetContentTypeQuery(id), cancellationToken);
        return OkOrProblem(result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(ContentTypeDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Create(
        [FromBody] CreateContentTypeCommand command,
   CancellationToken cancellationToken = default)
    {
        var result = await Sender.Send(command, cancellationToken);
        return CreatedOrProblem(result, nameof(Get), new { id = result.IsSuccess ? result.Value.Id : Guid.Empty });
    }

    [HttpPost("{id:guid}/fields")]
 [ProducesResponseType(typeof(ContentTypeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddField(
        Guid id,
        [FromBody] AddFieldRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await Sender.Send(
       new AddFieldCommand(id, request.Handle, request.Label, request.FieldType,
          request.IsRequired, request.IsLocalized, request.IsUnique, request.Description),
            cancellationToken);
     return OkOrProblem(result);
  }

    [HttpDelete("{id:guid}/fields/{fieldId:guid}")]
    [ProducesResponseType(typeof(ContentTypeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveField(
      Guid id,
        Guid fieldId,
        CancellationToken cancellationToken = default)
    {
        var result = await Sender.Send(new RemoveFieldCommand(id, fieldId), cancellationToken);
     return OkOrProblem(result);
    }

    [HttpPost("{id:guid}/publish")]
    [ProducesResponseType(typeof(ContentTypeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Publish(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await Sender.Send(new PublishContentTypeCommand(id), cancellationToken);
   return OkOrProblem(result);
    }

    [HttpPost("{id:guid}/archive")]
    [ProducesResponseType(typeof(ContentTypeDto), StatusCodes.Status200OK)]
 [ProducesResponseType(StatusCodes.Status404NotFound)]
  public async Task<IActionResult> Archive(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await Sender.Send(new ArchiveContentTypeCommand(id), cancellationToken);
        return OkOrProblem(result);
    }
}

public sealed record AddFieldRequest(
    string Handle,
    string Label,
    string FieldType,
    bool IsRequired = false,
    bool IsLocalized = false,
    bool IsUnique = false,
    string? Description = null);
