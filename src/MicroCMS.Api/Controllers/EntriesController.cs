using System.Text.Json;
using MicroCMS.Application.Features.Entries.Commands.Bulk;
using MicroCMS.Application.Features.Entries.Commands.CancelScheduledPublish;
using MicroCMS.Application.Features.Entries.Commands.CreateEntry;
using MicroCMS.Application.Features.Entries.Commands.DeleteEntry;
using MicroCMS.Application.Features.Entries.Commands.PublishEntry;
using MicroCMS.Application.Features.Entries.Commands.RollbackEntryVersion;
using MicroCMS.Application.Features.Entries.Commands.SchedulePublish;
using MicroCMS.Application.Features.Entries.Commands.UnpublishEntry;
using MicroCMS.Application.Features.Entries.Commands.UpdateEntry;
using MicroCMS.Application.Features.Entries.Commands.Workflow;
using MicroCMS.Application.Features.Entries.Dtos;
using MicroCMS.Application.Features.Entries.Queries.ExportEntries;
using MicroCMS.Application.Features.Entries.Queries.GetEntry;
using MicroCMS.Application.Features.Entries.Queries.GetEntryVersions;
using MicroCMS.Application.Features.Entries.Queries.ListEntries;
using MicroCMS.Application.Features.Entries.Queries.Preview;
using MicroCMS.Application.Features.Entries.Queries.QualityChecks;
using MicroCMS.Shared.Primitives;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MicroCMS.Api.Controllers;

/// <summary>Content entry CRUD, workflow, versioning, scheduling, SEO, bulk ops, export, and preview.</summary>
[Authorize]
public sealed class EntriesController : ApiControllerBase
{
    private static readonly JsonSerializerOptions _jsonOpts = new() { PropertyNameCaseInsensitive = true };

    // ── Queries ───────────────────────────────────────────────────────────

    [HttpGet]
    [ProducesResponseType(typeof(PagedList<EntryListItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(
        [FromQuery] Guid siteId,
        [FromQuery] string? status,
        [FromQuery] Guid? contentTypeId,
        [FromQuery] string? locale,
        [FromQuery] Guid? folderId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default) =>
        OkOrProblem(await Sender.Send(
            new ListEntriesQuery(siteId, status, contentTypeId, locale, folderId, pageNumber, pageSize),
            cancellationToken));

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(EntryDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken = default) =>
        OkOrProblem(await Sender.Send(new GetEntryQuery(id), cancellationToken));

    [HttpGet("{id:guid}/versions")]
    [ProducesResponseType(typeof(IReadOnlyList<EntryVersionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetVersions(Guid id, CancellationToken cancellationToken = default) =>
        OkOrProblem(await Sender.Send(new GetEntryVersionsQuery(id), cancellationToken));

    [HttpGet("{id:guid}/quality-checks")]
    [ProducesResponseType(typeof(QualityCheckReport), StatusCodes.Status200OK)]
    public async Task<IActionResult> QualityChecks(Guid id, CancellationToken cancellationToken = default) =>
        OkOrProblem(await Sender.Send(new RunQualityChecksQuery(id), cancellationToken));

    [HttpGet("{id:guid}/preview-token")]
    [ProducesResponseType(typeof(PreviewTokenResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPreviewToken(Guid id, CancellationToken cancellationToken = default) =>
        OkOrProblem(await Sender.Send(new GeneratePreviewTokenQuery(id), cancellationToken));

    [HttpGet("preview")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(EntryDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByPreviewToken(
        [FromQuery] string token, CancellationToken cancellationToken = default) =>
        OkOrProblem(await Sender.Send(new GetEntryByPreviewTokenQuery(token), cancellationToken));

    [HttpGet("export")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Export(
        [FromQuery] Guid siteId, [FromQuery] Guid? contentTypeId,
        [FromQuery] ExportFormat format = ExportFormat.Json,
        CancellationToken cancellationToken = default)
    {
        var result = await Sender.Send(new ExportEntriesQuery(siteId, contentTypeId, format), cancellationToken);
        if (result.IsFailure) return ToProblemResult(result.Error);
        return File(result.Value.Data, result.Value.ContentType, result.Value.FileName);
    }

    // ── Commands ──────────────────────────────────────────────────────────

    [HttpPost]
    [ProducesResponseType(typeof(EntryDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] CreateEntryRequest r, CancellationToken ct = default)
    {
        var fieldsJson = SerialiseFields(r.Fields);
        var command = new CreateEntryCommand(r.SiteId, r.ContentTypeId, r.Slug, r.Locale, fieldsJson);
        var result = await Sender.Send(command, ct);
        return CreatedOrProblem(result, nameof(Get), new { id = result.IsSuccess ? result.Value.Id : Guid.Empty });
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(EntryDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Update(
        Guid id, [FromBody] UpdateEntryRequest r, CancellationToken ct = default) =>
        OkOrProblem(await Sender.Send(
            new UpdateEntryCommand(id, SerialiseFields(r.Fields), r.NewSlug, r.ChangeNote), ct));

    [HttpPost("{id:guid}/publish")]
    public async Task<IActionResult> Publish(Guid id, CancellationToken ct = default) =>
        OkOrProblem(await Sender.Send(new PublishEntryCommand(id), ct));

    [HttpPost("{id:guid}/unpublish")]
    public async Task<IActionResult> Unpublish(Guid id, CancellationToken ct = default) =>
        OkOrProblem(await Sender.Send(new UnpublishEntryCommand(id), ct));

    [HttpPost("{id:guid}/schedule")]
    public async Task<IActionResult> Schedule(
        Guid id, [FromBody] ScheduleEntryRequest r, CancellationToken ct = default) =>
        OkOrProblem(await Sender.Send(new SchedulePublishCommand(id, r.PublishAt, r.UnpublishAt), ct));

    [HttpDelete("{id:guid}/schedule")]
    public async Task<IActionResult> CancelSchedule(Guid id, CancellationToken ct = default) =>
        OkOrProblem(await Sender.Send(new CancelScheduledPublishCommand(id), ct));

    [HttpPost("{id:guid}/submit")]
    public async Task<IActionResult> Submit(Guid id, CancellationToken ct = default) =>
        OkOrProblem(await Sender.Send(new SubmitForReviewCommand(id), ct));

    [HttpPost("{id:guid}/approve")]
    public async Task<IActionResult> Approve(Guid id, CancellationToken ct = default) =>
        OkOrProblem(await Sender.Send(new ApproveEntryCommand(id), ct));

    [HttpPost("{id:guid}/reject")]
    public async Task<IActionResult> Reject(
        Guid id, [FromBody] RejectRequest r, CancellationToken ct = default) =>
        OkOrProblem(await Sender.Send(new RejectEntryCommand(id, r.Reason), ct));

    /// <summary>
    /// Rolls back to a specific version identified by its GUID.
    /// Loads all versions of the entry, resolves the version number, then delegates
    /// to <see cref="RollbackEntryVersionCommand"/>.
    /// </summary>
    [HttpPost("{id:guid}/versions/{versionId:guid}/restore")]
    [ProducesResponseType(typeof(EntryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RestoreVersion(
        Guid id, Guid versionId, CancellationToken ct = default)
    {
        var versionsResult = await Sender.Send(new GetEntryVersionsQuery(id), ct);
        if (versionsResult.IsFailure)
            return ToProblemResult(versionsResult.Error);

        var version = versionsResult.Value.FirstOrDefault(v => v.Id == versionId);
        if (version is null)
            return NotFound(new { detail = $"Version {versionId} not found on entry {id}." });

        return OkOrProblem(await Sender.Send(new RollbackEntryVersionCommand(id, version.VersionNumber), ct));
    }

    /// <summary>Rollback by version number (kept for backward compatibility).</summary>
    [HttpPost("{id:guid}/rollback")]
    public async Task<IActionResult> Rollback(
        Guid id, [FromBody] RollbackEntryRequest r, CancellationToken ct = default) =>
        OkOrProblem(await Sender.Send(new RollbackEntryVersionCommand(id, r.TargetVersionNumber), ct));

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct = default) =>
        NoContentOrProblem(await Sender.Send(new DeleteEntryCommand(id), ct));

    // ── Bulk operations ───────────────────────────────────────────────────

    [HttpPost("bulk/publish")]
    [ProducesResponseType(typeof(BulkOperationResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> BulkPublish(
        [FromBody] BulkEntriesRequest r, CancellationToken ct = default) =>
        OkOrProblem(await Sender.Send(new BulkPublishEntriesCommand(r.EntryIds), ct));

    [HttpPost("bulk/unpublish")]
    [ProducesResponseType(typeof(BulkOperationResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> BulkUnpublish(
        [FromBody] BulkEntriesRequest r, CancellationToken ct = default) =>
        OkOrProblem(await Sender.Send(new BulkUnpublishEntriesCommand(r.EntryIds), ct));

    [HttpDelete("bulk")]
    [ProducesResponseType(typeof(BulkOperationResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> BulkDelete(
        [FromBody] BulkEntriesRequest r, CancellationToken ct = default) =>
        OkOrProblem(await Sender.Send(new BulkDeleteEntriesCommand(r.EntryIds), ct));

    // ── Helpers ───────────────────────────────────────────────────────────

    /// <summary>Serialises the structured fields map back to a JSON string for the domain command.</summary>
    private static string SerialiseFields(Dictionary<string, JsonElement>? fields) =>
        fields is null || fields.Count == 0
            ? "{}"
            : JsonSerializer.Serialize(fields, _jsonOpts);
}

// ── Request models ─────────────────────────────────────────────────────────────

/// <summary>Payload for POST /entries. Fields are a structured JSON object.</summary>
public sealed record CreateEntryRequest(
    Guid SiteId,
    Guid ContentTypeId,
    string Slug,
    string Locale,
    Dictionary<string, JsonElement>? Fields = null);

/// <summary>Payload for PUT /entries/{id}. Fields are a structured JSON object.</summary>
public sealed record UpdateEntryRequest(
    Dictionary<string, JsonElement>? Fields,
    string? NewSlug = null,
    string? ChangeNote = null);

public sealed record ScheduleEntryRequest(DateTimeOffset PublishAt, DateTimeOffset? UnpublishAt = null);
public sealed record RollbackEntryRequest(int TargetVersionNumber);
public sealed record RejectRequest(string Reason);
public sealed record BulkEntriesRequest(IReadOnlyList<Guid> EntryIds);
