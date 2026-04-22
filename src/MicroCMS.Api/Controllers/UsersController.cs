using MicroCMS.Application.Features.Users.Commands;
using MicroCMS.Application.Features.Users.Dtos;
using MicroCMS.Application.Features.Users.Queries;
using MicroCMS.Shared.Primitives;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MicroCMS.Api.Controllers;

/// <summary>
/// User management within the current tenant.
/// Requires <c>UserManage</c> policy (TenantAdmin or SystemAdmin roles).
/// </summary>
[Authorize]
public sealed class UsersController : ApiControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(PagedList<UserListItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(
  [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await Sender.Send(new ListUsersQuery(page, pageSize), cancellationToken);
 return OkOrProblem(result);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken = default)
{
        var result = await Sender.Send(new GetUserQuery(id), cancellationToken);
        return OkOrProblem(result);
    }

    /// <summary>Invites a new user into the current tenant.</summary>
    [HttpPost("invite")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Invite(
    [FromBody] InviteUserCommand command,
    CancellationToken cancellationToken = default)
    {
        var result = await Sender.Send(command, cancellationToken);
  return CreatedOrProblem(result, nameof(Get), new { id = result.IsSuccess ? result.Value.Id : Guid.Empty });
    }

    /// <summary>Assigns a workflow role to a user.</summary>
    [HttpPost("{id:guid}/roles")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AssignRole(
        Guid id,
  [FromBody] AssignRoleRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await Sender.Send(new AssignRoleCommand(id, request.WorkflowRole, request.SiteId), cancellationToken);
        return OkOrProblem(result);
    }

    /// <summary>Revokes a specific role from a user.</summary>
    [HttpDelete("{id:guid}/roles/{roleId:guid}")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
 [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RevokeRole(
        Guid id,
     Guid roleId,
        CancellationToken cancellationToken = default)
  {
     var result = await Sender.Send(new RevokeRoleCommand(id, roleId), cancellationToken);
   return OkOrProblem(result);
}

    /// <summary>Deactivates a user, preventing login and role assignments.</summary>
    [HttpPost("{id:guid}/deactivate")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await Sender.Send(new DeactivateUserCommand(id), cancellationToken);
     return OkOrProblem(result);
    }
}

public sealed record AssignRoleRequest(string WorkflowRole, Guid? SiteId = null);
