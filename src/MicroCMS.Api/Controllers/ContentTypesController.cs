using MicroCMS.Application.Features.ContentTypes.Commands;
using MicroCMS.Application.Features.ContentTypes.Dtos;
using MicroCMS.Application.Features.ContentTypes.Queries;
using MicroCMS.Domain.Enums;
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
     [FromBody] CreateContentTypeRequest request,
    CancellationToken cancellationToken = default)
    {
        if (!Enum.TryParse<LocalizationMode>(request.LocalizationMode, ignoreCase: true, out var locMode))
     locMode = LocalizationMode.PerLocale;

        var result = await Sender.Send(
      new CreateContentTypeCommand(request.SiteId, request.Handle, request.DisplayName,
                request.Description, locMode),
      cancellationToken);
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
     new AddFieldCommand(
   id, request.Handle, request.Label, request.FieldType,
     request.IsRequired, request.IsLocalized, request.IsUnique,
 request.IsIndexed, request.Description),
cancellationToken);
        return OkOrProblem(result);
    }

    [HttpDelete("{id:guid}/fields/{fieldId:guid}")]
    [ProducesResponseType(typeof(ContentTypeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveField(Guid id, Guid fieldId, CancellationToken cancellationToken = default)
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

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ContentTypeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateContentTypeRequest request,
        CancellationToken cancellationToken = default)
    {
  LocalizationMode? locMode = null;
        if (request.LocalizationMode is not null &&
    Enum.TryParse<LocalizationMode>(request.LocalizationMode, ignoreCase: true, out var parsed))
            locMode = parsed;

 var fields = request.Fields?
            .Select(f => new UpdateFieldInput(
                f.Id, f.Handle, f.Label, f.FieldType,
            f.IsRequired, f.IsLocalized, f.IsUnique, f.IsIndexed, f.SortOrder, f.Description))
            .ToList();

 var result = await Sender.Send(
   new UpdateContentTypeCommand(id, request.DisplayName, request.Description, locMode, fields),
      cancellationToken);
 return OkOrProblem(result);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
  [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken = default)
    {
    var result = await Sender.Send(new DeleteContentTypeCommand(id), cancellationToken);
        return NoContentOrProblem(result);
    }

    /// <summary>
    /// Imports a content type schema from a structured JSON payload.
    /// Capped at 50 fields. Requires ContentAdmin role.
    /// </summary>
    [HttpPost("import")]
    [ProducesResponseType(typeof(ContentTypeDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> ImportSchema(
     [FromBody] ImportSchemaRequest request,
    CancellationToken cancellationToken = default)
    {
        if (request.Fields?.Count > 50)
   return UnprocessableEntity(new { detail = "Import is limited to 50 fields." });

   var fields = (request.Fields ?? [])
          .Select(f => new ImportFieldInput(f.Handle, f.Label, f.FieldType,
       f.IsRequired, f.IsLocalized))
     .ToList();

 var result = await Sender.Send(
     new ImportContentTypeSchemaCommand(
    request.SiteId, request.Handle, request.DisplayName,
    request.Description, fields),
         cancellationToken);
   return CreatedOrProblem(result, nameof(Get), new { id = result.IsSuccess ? result.Value.Id : Guid.Empty });
    }
}

// ── Request models ───────────────────────────────────────────────────────────

public sealed record CreateContentTypeRequest(
    Guid SiteId,
    string Handle,
    string DisplayName,
    string? Description = null,
    string? LocalizationMode = null);

public sealed record AddFieldRequest(
    string Handle,
    string Label,
    string FieldType,
    bool IsRequired = false,
    bool IsLocalized = false,
    bool IsUnique = false,
    bool IsIndexed = false,
    string? Description = null);

public sealed record UpdateContentTypeRequest(
    string DisplayName,
string? Description = null,
  string? LocalizationMode = null,
    IReadOnlyList<UpdateFieldRequest>? Fields = null);

public sealed record UpdateFieldRequest(
Guid? Id,
    string Handle,
    string Label,
    string FieldType,
    bool IsRequired = false,
    bool IsLocalized = false,
    bool IsUnique = false,
    bool IsIndexed = false,
    int SortOrder = 0,
    string? Description = null);

public sealed record ImportSchemaRequest(
    Guid SiteId,
    string Handle,
    string DisplayName,
    string? Description = null,
    IReadOnlyList<ImportSchemaFieldRequest>? Fields = null);

public sealed record ImportSchemaFieldRequest(
  string Handle,
    string Label,
    string FieldType,
    bool IsRequired = false,
    bool IsLocalized = false);
