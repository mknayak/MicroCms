using MicroCMS.Application.Features.ApiClients.Commands;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MicroCMS.Api.Controllers;

/// <summary>API client (delivery/management/preview key) management (GAP-20).</summary>
[Authorize]
public sealed class ApiClientsController : ApiControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(ApiClientCreatedDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create(
   [FromBody] CreateApiClientCommand command, CancellationToken ct = default)
{
  var result = await Sender.Send(command, ct);
        return result.IsSuccess
     ? StatusCode(StatusCodes.Status201Created, result.Value)
   : ToProblemResult(result.Error);
    }

    [HttpPost("{id:guid}/revoke")]
    [ProducesResponseType(typeof(ApiClientDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Revoke(Guid id, CancellationToken ct = default) =>
        OkOrProblem(await Sender.Send(new RevokeApiClientCommand(id), ct));

    [HttpPost("{id:guid}/regenerate")]
    [ProducesResponseType(typeof(ApiClientCreatedDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Regenerate(Guid id, CancellationToken ct = default) =>
        OkOrProblem(await Sender.Send(new RegenerateApiClientCommand(id), ct));
}
