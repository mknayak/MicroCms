using MicroCMS.Application.Features.Locks.Commands;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MicroCMS.Api.Controllers;

/// <summary>
/// Manages pessimistic edit locks. A lock prevents concurrent editing of the same entity.
/// Locks expire after 30 minutes of inactivity and can be refreshed by the lock owner.
/// </summary>
[Authorize]
public sealed class LocksController : ApiControllerBase
{
    /// <summary>Gets the current lock for an entity. Returns null if unlocked or expired.</summary>
    [HttpGet("{entityId}")]
    [ProducesResponseType(typeof(EditLockDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Get(string entityId, CancellationToken ct = default) =>
        OkOrProblem(await Sender.Send(new GetLockQuery(entityId), ct));

    /// <summary>
    /// Acquires a lock on an entity for the current user.
    /// Returns 409 Conflict if locked by another user.
    /// </summary>
[HttpPost("acquire")]
    [ProducesResponseType(typeof(EditLockDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Acquire([FromBody] AcquireLockCommand command, CancellationToken ct = default) =>
        OkOrProblem(await Sender.Send(command, ct));

    /// <summary>Releases the lock. Only the lock owner or a SystemAdmin can release.</summary>
    [HttpDelete("{entityId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Release(string entityId, CancellationToken ct = default) =>
     NoContentOrProblem(await Sender.Send(new ReleaseLockCommand(entityId), ct));

  /// <summary>Refreshes the TTL for a lock owned by the current user.</summary>
    [HttpPost("{entityId}/refresh")]
    [ProducesResponseType(typeof(EditLockDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Refresh(string entityId, CancellationToken ct = default) =>
        OkOrProblem(await Sender.Send(new RefreshLockCommand(entityId), ct));
}
