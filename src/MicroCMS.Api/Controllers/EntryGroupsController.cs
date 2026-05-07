using Asp.Versioning;
using MicroCMS.Application.Features.EntryGroups.Commands;
using MicroCMS.Application.Features.EntryGroups.Dtos;
using MicroCMS.Application.Features.EntryGroups.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace MicroCMS.Api.Controllers;

/// <summary>
/// Entry group management scoped to a content type.
/// Exposed as a nested resource under <c>/api/v1/content-types/{contentTypeId}/groups</c>
/// so the admin UI Groups tab can bind directly to the content type detail page.
/// </summary>
[Authorize]
[Route("api/v{version:apiVersion}/content-types/{contentTypeId:guid}/groups")]
[ApiController]
[ApiVersion("1.0")]
[Produces("application/json")]
public sealed class EntryGroupsController : ApiControllerBase
{
    // ── Queries ───────────────────────────────────────────────────────────

    /// <summary>Lists all groups for a content type (Groups tab data source).</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<EntryGroupListItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(
        Guid contentTypeId,
        CancellationToken cancellationToken = default) =>
        OkOrProblem(await Sender.Send(new ListEntryGroupsQuery(contentTypeId), cancellationToken));

    /// <summary>Returns a single group with its full member list.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(EntryGroupDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(
        Guid contentTypeId,
        Guid id,
        CancellationToken cancellationToken = default) =>
        OkOrProblem(await Sender.Send(new GetEntryGroupQuery(id), cancellationToken));

    // ── Commands ──────────────────────────────────────────────────────────

    /// <summary>Creates a new entry group for this content type.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(EntryGroupDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Create(
        Guid contentTypeId,
        [FromBody] CreateEntryGroupRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await Sender.Send(
            new CreateEntryGroupCommand(
                contentTypeId,
                request.Handle,
                request.Title,
                request.Description,
                request.ImageAssetId,
                request.MemberEntryIds),
            cancellationToken);

        return CreatedOrProblem(result, nameof(Get),
            new { contentTypeId, id = result.IsSuccess ? result.Value.Id : Guid.Empty });
    }

    /// <summary>Updates a group's title, description, image, and/or member list.</summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(EntryGroupDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        Guid contentTypeId,
        Guid id,
        [FromBody] UpdateEntryGroupRequest request,
        CancellationToken cancellationToken = default) =>
        OkOrProblem(await Sender.Send(
            new UpdateEntryGroupCommand(id, request.Title, request.Description, request.ImageAssetId, request.MemberEntryIds),
            cancellationToken));

    /// <summary>Deletes a group. Member entries are not affected.</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(
        Guid contentTypeId,
        Guid id,
        CancellationToken cancellationToken = default) =>
        NoContentOrProblem(await Sender.Send(new DeleteEntryGroupCommand(id), cancellationToken));
}

// ── Request bodies ─────────────────────────────────────────────────────────────

public sealed record CreateEntryGroupRequest(
    string Handle,
    string Title,
    string? Description = null,
    Guid? ImageAssetId = null,
    IReadOnlyList<Guid>? MemberEntryIds = null);

public sealed record UpdateEntryGroupRequest(
    string Title,
    string? Description = null,
    Guid? ImageAssetId = null,
    IReadOnlyList<Guid>? MemberEntryIds = null);
