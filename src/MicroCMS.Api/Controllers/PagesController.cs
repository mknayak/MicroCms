using MicroCMS.Application.Features.Pages.Commands;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MicroCMS.Api.Controllers;

/// <summary>Page tree CRUD and site structure management (GAP-21).</summary>
[Authorize]
public sealed class PagesController : ApiControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<PageTreeNode>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTree(
 [FromQuery] Guid siteId, CancellationToken ct = default) =>
  OkOrProblem(await Sender.Send(new GetSiteTreeQuery(siteId), ct));

    [HttpPost("static")]
    [ProducesResponseType(typeof(PageDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateStatic(
  [FromBody] CreateStaticPageCommand command, CancellationToken ct = default)
    {
        var result = await Sender.Send(command, ct);
   return CreatedOrProblem(result, nameof(GetTree), new { siteId = command.SiteId });
}

    [HttpPost("collection")]
    [ProducesResponseType(typeof(PageDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateCollection(
    [FromBody] CreateCollectionPageCommand command, CancellationToken ct = default)
    {
  var result = await Sender.Send(command, ct);
   return CreatedOrProblem(result, nameof(GetTree), new { siteId = command.SiteId });
  }

    [HttpPut("{id:guid}/move")]
    public async Task<IActionResult> Move(
    Guid id, [FromBody] MovePageRequest r, CancellationToken ct = default) =>
   OkOrProblem(await Sender.Send(new MovePageCommand(id, r.NewParentId), ct));

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct = default) =>
        NoContentOrProblem(await Sender.Send(new DeletePageCommand(id), ct));
}

public sealed record MovePageRequest(Guid? NewParentId);
