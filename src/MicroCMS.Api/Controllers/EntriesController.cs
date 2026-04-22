using MicroCMS.Application.Features.Entries.Commands.Bulk;
using MicroCMS.Application.Features.Entries.Commands.CancelScheduledPublish;
using MicroCMS.Application.Features.Entries.Commands.CreateEntry;
using MicroCMS.Application.Features.Entries.Commands.DeleteEntry;
using MicroCMS.Application.Features.Entries.Commands.PublishEntry;
using MicroCMS.Application.Features.Entries.Commands.RollbackEntryVersion;
using MicroCMS.Application.Features.Entries.Commands.SchedulePublish;
using MicroCMS.Application.Features.Entries.Commands.UnpublishEntry;
using MicroCMS.Application.Features.Entries.Commands.UpdateEntry;
using MicroCMS.Application.Features.Entries.Commands.UpdateSeoMetadata;
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
    // ── Queries ───────────────────────────────────────────────────────────

    [HttpGet]
    [ProducesResponseType(typeof(PagedList<EntryListItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(
  [FromQuery] Guid siteId, [FromQuery] string? status,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default) =>
        OkOrProblem(await Sender.Send(new ListEntriesQuery(siteId, status, page, pageSize), cancellationToken));

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
    public async Task<IActionResult> Create([FromBody] CreateEntryCommand command, CancellationToken ct = default)
    {
        var result = await Sender.Send(command, ct);
   return CreatedOrProblem(result, nameof(Get), new { id = result.IsSuccess ? result.Value.Id : Guid.Empty });
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(EntryDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Update(
        Guid id, [FromBody] UpdateEntryRequest r, CancellationToken ct = default) =>
        OkOrProblem(await Sender.Send(new UpdateEntryCommand(id, r.FieldsJson, r.NewSlug, r.ChangeNote), ct));

    [HttpPut("{id:guid}/seo")]
    [ProducesResponseType(typeof(EntryDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateSeo(
 Guid id, [FromBody] UpdateSeoRequest r, CancellationToken ct = default) =>
        OkOrProblem(await Sender.Send(new UpdateSeoMetadataCommand(id, r.MetaTitle, r.MetaDescription, r.CanonicalUrl, r.OgImage), ct));

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
}

// ── Request models ─────────────────────────────────────────────────────────────
public sealed record UpdateEntryRequest(string FieldsJson, string? NewSlug = null, string? ChangeNote = null);
public sealed record ScheduleEntryRequest(DateTimeOffset PublishAt, DateTimeOffset? UnpublishAt = null);
public sealed record RollbackEntryRequest(int TargetVersionNumber);
public sealed record UpdateSeoRequest(string? MetaTitle, string? MetaDescription, string? CanonicalUrl = null, string? OgImage = null);
public sealed record RejectRequest(string Reason);
public sealed record BulkEntriesRequest(IReadOnlyList<Guid> EntryIds);
