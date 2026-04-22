using MicroCMS.Application.Features.Media.Commands;
using MicroCMS.Application.Features.Media.Dtos;
using MicroCMS.Application.Features.Media.Queries;
using MicroCMS.Shared.Primitives;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MicroCMS.Api.Controllers;

/// <summary>Media asset registration, metadata, and deletion.</summary>
[Authorize]
public sealed class MediaController : ApiControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(PagedList<MediaAssetListItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(
        [FromQuery] Guid siteId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
      CancellationToken cancellationToken = default)
    {
        var result = await Sender.Send(new ListMediaAssetsQuery(siteId, page, pageSize), cancellationToken);
        return OkOrProblem(result);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(MediaAssetDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await Sender.Send(new GetMediaAssetQuery(id), cancellationToken);
        return OkOrProblem(result);
    }

    /// <summary>
    /// Registers a media asset after the binary has been stored by the caller.
    /// Full multipart streaming upload arrives in Sprint 7.
/// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(MediaAssetDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Register(
    [FromBody] RegisterMediaAssetCommand command,
        CancellationToken cancellationToken = default)
    {
     var result = await Sender.Send(command, cancellationToken);
        return CreatedOrProblem(result, nameof(Get), new { id = result.IsSuccess ? result.Value.Id : Guid.Empty });
    }

    [HttpPatch("{id:guid}/metadata")]
    [ProducesResponseType(typeof(MediaAssetDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateMetadata(
  Guid id,
        [FromBody] UpdateMediaMetadataRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await Sender.Send(
  new UpdateMediaAssetMetadataCommand(id, request.AltText, request.Tags),
  cancellationToken);
        return OkOrProblem(result);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await Sender.Send(new DeleteMediaAssetCommand(id), cancellationToken);
  return NoContentOrProblem(result);
 }
}

public sealed record UpdateMediaMetadataRequest(string? AltText, IReadOnlyList<string>? Tags);
