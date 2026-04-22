using MicroCMS.Application.Features.Webhooks.Commands;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MicroCMS.Api.Controllers;

/// <summary>Webhook subscription management (GAP-19).</summary>
[Authorize]
public sealed class WebhooksController : ApiControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(WebhookSubscriptionDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create(
 [FromBody] CreateWebhookRequest r, CancellationToken ct = default)
  {
  var result = await Sender.Send(
   new CreateWebhookCommand(r.SiteId, r.TargetUrl, r.HashedSecret, r.Events, r.MaxRetries), ct);
        return result.IsSuccess
      ? StatusCode(StatusCodes.Status201Created, result.Value)
   : ToProblemResult(result.Error);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id, [FromBody] UpdateWebhookRequest r, CancellationToken ct = default) =>
        OkOrProblem(await Sender.Send(new UpdateWebhookCommand(id, r.Events, r.IsActive), ct));

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct = default) =>
        NoContentOrProblem(await Sender.Send(new DeleteWebhookCommand(id), ct));
}

public sealed record CreateWebhookRequest(
    Guid? SiteId, string TargetUrl, string HashedSecret,
    IReadOnlyList<string> Events, int MaxRetries = 3);

public sealed record UpdateWebhookRequest(IReadOnlyList<string> Events, bool IsActive);
