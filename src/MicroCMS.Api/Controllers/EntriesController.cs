using MicroCMS.Application.Features.Entries.Commands.CreateEntry;
using MicroCMS.Application.Features.Entries.Commands.DeleteEntry;
using MicroCMS.Application.Features.Entries.Commands.PublishEntry;
using MicroCMS.Application.Features.Entries.Commands.RollbackEntryVersion;
using MicroCMS.Application.Features.Entries.Commands.SchedulePublish;
using MicroCMS.Application.Features.Entries.Commands.UnpublishEntry;
using MicroCMS.Application.Features.Entries.Commands.UpdateEntry;
using MicroCMS.Application.Features.Entries.Dtos;
using MicroCMS.Application.Features.Entries.Queries.GetEntry;
using MicroCMS.Application.Features.Entries.Queries.GetEntryVersions;
using MicroCMS.Application.Features.Entries.Queries.ListEntries;
using MicroCMS.Shared.Primitives;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MicroCMS.Api.Controllers;

/// <summary>Content entry CRUD, workflow, version history, and scheduling.</summary>
[Authorize]
public sealed class EntriesController : ApiControllerBase
{
    // ── Queries ───────────────────────────────────────────────────────────

    /// <summary>Returns a paginated list of entries for a site.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedList<EntryListItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(
        [FromQuery] Guid siteId,
     [FromQuery] string? status = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await Sender.Send(new ListEntriesQuery(siteId, status, page, pageSize), cancellationToken);
        return OkOrProblem(result);
    }

    /// <summary>Returns a single entry by ID.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(EntryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken = default)
    {
      var result = await Sender.Send(new GetEntryQuery(id), cancellationToken);
     return OkOrProblem(result);
    }

    /// <summary>Returns the version history of an entry.</summary>
    [HttpGet("{id:guid}/versions")]
    [ProducesResponseType(typeof(IReadOnlyList<EntryVersionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetVersions(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await Sender.Send(new GetEntryVersionsQuery(id), cancellationToken);
     return OkOrProblem(result);
    }

    // ── Commands ──────────────────────────────────────────────────────────

    /// <summary>Creates a new entry in Draft status.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(EntryDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Create(
   [FromBody] CreateEntryCommand command,
        CancellationToken cancellationToken = default)
    {
        var result = await Sender.Send(command, cancellationToken);
 return CreatedOrProblem(result, nameof(Get), new { id = result.IsSuccess ? result.Value.Id : Guid.Empty });
    }

    /// <summary>Updates entry field data and optionally its slug.</summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(EntryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateEntryRequest request,
  CancellationToken cancellationToken = default)
    {
        var result = await Sender.Send(
          new UpdateEntryCommand(id, request.FieldsJson, request.NewSlug, request.ChangeNote),
          cancellationToken);
        return OkOrProblem(result);
    }

    /// <summary>Publishes an Approved or Scheduled entry immediately.</summary>
    [HttpPost("{id:guid}/publish")]
    [ProducesResponseType(typeof(EntryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Publish(Guid id, CancellationToken cancellationToken = default)
  {
        var result = await Sender.Send(new PublishEntryCommand(id), cancellationToken);
        return OkOrProblem(result);
    }

    /// <summary>Moves a Published entry to Unpublished.</summary>
    [HttpPost("{id:guid}/unpublish")]
    [ProducesResponseType(typeof(EntryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Unpublish(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await Sender.Send(new UnpublishEntryCommand(id), cancellationToken);
        return OkOrProblem(result);
    }

    /// <summary>Schedules an Approved entry for future publication.</summary>
    [HttpPost("{id:guid}/schedule")]
    [ProducesResponseType(typeof(EntryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Schedule(
        Guid id,
        [FromBody] ScheduleEntryRequest request,
    CancellationToken cancellationToken = default)
    {
        var result = await Sender.Send(
            new SchedulePublishCommand(id, request.PublishAt, request.UnpublishAt),
 cancellationToken);
        return OkOrProblem(result);
    }

    /// <summary>Rolls an entry back to a previous version snapshot.</summary>
    [HttpPost("{id:guid}/rollback")]
    [ProducesResponseType(typeof(EntryDto), StatusCodes.Status200OK)]
  [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Rollback(
        Guid id,
    [FromBody] RollbackEntryRequest request,
  CancellationToken cancellationToken = default)
    {
        var result = await Sender.Send(
     new RollbackEntryVersionCommand(id, request.TargetVersionNumber),
            cancellationToken);
        return OkOrProblem(result);
    }

    /// <summary>Permanently deletes an entry and all its versions.</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await Sender.Send(new DeleteEntryCommand(id), cancellationToken);
        return NoContentOrProblem(result);
    }
}

// ── Request body models ────────────────────────────────────────────────────

public sealed record UpdateEntryRequest(string FieldsJson, string? NewSlug = null, string? ChangeNote = null);
public sealed record ScheduleEntryRequest(DateTimeOffset PublishAt, DateTimeOffset? UnpublishAt = null);
public sealed record RollbackEntryRequest(int TargetVersionNumber);
