using MicroCMS.Application.Features.Folders.Commands;
using MicroCMS.Application.Features.Folders.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MicroCMS.Api.Controllers;

/// <summary>Content folder management and tree query (GAP-02).</summary>
[Authorize]
public sealed class FoldersController : ApiControllerBase
{
  [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<FolderTreeNode>), StatusCodes.Status200OK)]
  public async Task<IActionResult> GetTree(
 [FromQuery] Guid siteId, CancellationToken ct = default) =>
  OkOrProblem(await Sender.Send(new GetFolderTreeQuery(siteId), ct));

 [HttpPost]
    [ProducesResponseType(typeof(FolderDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create(
  [FromBody] CreateFolderCommand command, CancellationToken ct = default)
    {
       var result = await Sender.Send(command, ct);
    return CreatedOrProblem(result, nameof(GetTree), new { siteId = command.SiteId });
  }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(FolderDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Rename(
        Guid id, [FromBody] RenameFolderRequest r, CancellationToken ct = default) =>
   OkOrProblem(await Sender.Send(new RenameFolderCommand(id, r.NewName), ct));

    [HttpPut("{id:guid}/move")]
    public async Task<IActionResult> Move(
     Guid id, [FromBody] MoveFolderRequest r, CancellationToken ct = default) =>
        OkOrProblem(await Sender.Send(new MoveFolderCommand(id, r.NewParentFolderId), ct));

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct = default) =>
        NoContentOrProblem(await Sender.Send(new DeleteFolderCommand(id), ct));
}

public sealed record RenameFolderRequest(string NewName);
public sealed record MoveFolderRequest(Guid? NewParentFolderId);
