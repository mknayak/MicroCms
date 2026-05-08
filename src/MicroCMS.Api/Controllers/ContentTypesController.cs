using MicroCMS.Application.Features.ContentTypes.Commands;
using MicroCMS.Application.Features.ContentTypes.Dtos;
using MicroCMS.Application.Features.ContentTypes.Queries;
using MicroCMS.Domain.Enums;
using MicroCMS.Shared.Primitives;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace MicroCMS.Api.Controllers;

/// <summary>Content type schema definition — create, manage fields, publish, archive.</summary>
[Authorize]
public sealed class ContentTypesController : ApiControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(PagedList<ContentTypeListItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
     CancellationToken cancellationToken = default)
    {
        var result = await Sender.Send(new ListContentTypesQuery(page, pageSize), cancellationToken);
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
    public async Task<IActionResult> Create(
   [FromBody] CreateContentTypeRequest request, CancellationToken cancellationToken = default)
    {
        if (!Enum.TryParse<LocalizationMode>(request.LocalizationMode, ignoreCase: true, out var locMode))
            locMode = LocalizationMode.PerLocale;

        var result = await Sender.Send(
      new CreateContentTypeCommand(request.Handle, request.DisplayName,
    request.Description, locMode, request.Kind ?? "Content", request.ParentContentTypeId),
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
 request.IsIndexed, request.IsList, request.Description,
        request.GroupName,
        request.Options,
  request.DynamicSource is null ? null : new FieldDynamicSourceInput(
      request.DynamicSource.ContentTypeHandle,
  request.DynamicSource.LabelField,
         request.DynamicSource.ValueField,
     request.DynamicSource.StatusFilter,
     request.DynamicSource.GroupHandle),
  request.MultiListSource is null ? null : new FieldDynamicSourceInput(
      request.MultiListSource.ContentTypeHandle,
      request.MultiListSource.LabelField,
      request.MultiListSource.ValueField,
      request.MultiListSource.StatusFilter,
      request.MultiListSource.GroupHandle)),
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
    public async Task<IActionResult> Update(
        Guid id, [FromBody] UpdateContentTypeRequest request, CancellationToken cancellationToken = default)
    {
        LocalizationMode? locMode = null;
        if (request.LocalizationMode is not null &&
       Enum.TryParse<LocalizationMode>(request.LocalizationMode, ignoreCase: true, out var parsed))
            locMode = parsed;

        var fields = request.Fields?
    .Select(f => new UpdateFieldInput(
   f.Id, f.Handle, f.Label, f.FieldType,
 f.IsRequired, f.IsLocalized, f.IsUnique, f.IsIndexed, f.IsList, f.SortOrder, f.Description,
  f.GroupName,
  f.Options,
        f.DynamicSource is null ? null : new FieldDynamicSourceInput(
            f.DynamicSource.ContentTypeHandle,
            f.DynamicSource.LabelField,
    f.DynamicSource.ValueField,
            f.DynamicSource.StatusFilter,
            f.DynamicSource.GroupHandle),
        f.MultiListSource is null ? null : new FieldDynamicSourceInput(
            f.MultiListSource.ContentTypeHandle,
            f.MultiListSource.LabelField,
            f.MultiListSource.ValueField,
            f.MultiListSource.StatusFilter,
            f.MultiListSource.GroupHandle)))
     .ToList();

        var result = await Sender.Send(
       new UpdateContentTypeCommand(id, request.DisplayName, request.Description, locMode,
           request.Kind, request.SiteTemplateId, fields,
           request.ParentContentTypeId, request.ClearParent),
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
           request.Handle, request.DisplayName,
           request.Description, fields),
                cancellationToken);
        return CreatedOrProblem(result, nameof(Get), new { id = result.IsSuccess ? result.Value.Id : Guid.Empty });
    }

    /// <summary>
    /// Resolves the effective option list for an Enum field.
    /// For static fields returns the stored options.
    /// For dynamic fields queries published entries of the source content type.
    /// Used by the schema designer (test button) and the entry editor (dropdown population).
    /// </summary>
    [HttpGet("{id:guid}/fields/{fieldId:guid}/enum-options")]
    [ProducesResponseType(typeof(IReadOnlyList<EnumOptionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetEnumOptions(
    Guid id, Guid fieldId,
        CancellationToken cancellationToken = default)
 {
        var result = await Sender.Send(new ResolveEnumOptionsQuery(id, fieldId), cancellationToken);
     return OkOrProblem(result);
    }

    /// <summary>
    /// Resolves the available (left-pane) entry list for a MultiList field.
    /// Optionally scoped to a group when the field's MultiListSource.GroupHandle is set.
    /// Used by the dual-pane MultiList picker in the entry editor.
    /// </summary>
    [HttpGet("{id:guid}/fields/{fieldId:guid}/multilist-options")]
    [ProducesResponseType(typeof(IReadOnlyList<MultiListOptionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMultiListOptions(
        Guid id, Guid fieldId,
        CancellationToken cancellationToken = default)
    {
        var result = await Sender.Send(new ResolveMultiListOptionsQuery(id, fieldId), cancellationToken);
        return OkOrProblem(result);
    }
}

// ── Request models ───────────────────────────────────────────────────────────

public sealed record CreateContentTypeRequest(
    string Handle,
    string DisplayName,
 string? Description = null,
    string? LocalizationMode = null,
    string? Kind = null,
    Guid? ParentContentTypeId = null);

public sealed record AddFieldRequest(
    string Handle,
    string Label,
    string FieldType,
    bool IsRequired = false,
    bool IsLocalized = false,
    bool IsUnique = false,
    bool IsIndexed = false,
    bool IsList = false,
    string? Description = null,
    string GroupName = "Default",
    /// <summary>Static option list for Enum fields.</summary>
    IReadOnlyList<string>? Options = null,
    /// <summary>Dynamic source config for Enum/Reference fields.</summary>
  FieldDynamicSourceRequest? DynamicSource = null,
    /// <summary>Source config for MultiList fields — defines the content type to pick entries from.</summary>
    FieldDynamicSourceRequest? MultiListSource = null);

public sealed record UpdateContentTypeRequest(
    string DisplayName,
    string? Description = null,
    string? LocalizationMode = null,
    string? Kind = null,
    Guid? SiteTemplateId = null,
    IReadOnlyList<UpdateFieldRequest>? Fields = null,
    Guid? ParentContentTypeId = null,
    bool ClearParent = false);

public sealed record UpdateFieldRequest(
Guid? Id,
    string Handle,
    string Label,
    string FieldType,
  bool IsRequired = false,
    bool IsLocalized = false,
    bool IsUnique = false,
    bool IsIndexed = false,
 bool IsList = false,
 int SortOrder = 0,
    string? Description = null,
    string GroupName = "Default",
    IReadOnlyList<string>? Options = null,
    FieldDynamicSourceRequest? DynamicSource = null,
    /// <summary>Source config for MultiList fields.</summary>
    FieldDynamicSourceRequest? MultiListSource = null);

public sealed record ImportSchemaRequest(
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

/// <summary>API request model for dynamic Enum/Reference/MultiList source configuration.</summary>
public sealed record FieldDynamicSourceRequest(
    string ContentTypeHandle,
    string LabelField = "title",
    string ValueField = "slug",
    string StatusFilter = "Published",
    /// <summary>When set, restricts available entries to members of this group handle.</summary>
    string? GroupHandle = null);
