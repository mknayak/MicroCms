using Asp.Versioning;
using MicroCMS.Application.Features.Install.Commands;
using MicroCMS.Application.Features.Install.Dtos;
using MicroCMS.Application.Features.Install.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MicroCMS.Api.Controllers;

/// <summary>
/// First-run installation endpoints.
///
/// <list type="bullet">
///   <item><term>GET  /api/v1/install/status</term><description>Returns whether the system has been installed.</description></item>
///   <item><term>POST /api/v1/install</term><description>Performs first-run installation (tenant + admin user).</description></item>
/// </list>
///
/// Both endpoints are <b>anonymous</b> — authentication is not possible before the system is installed.
/// <c>InstallationGuardMiddleware</c> ensures that <c>POST /api/v1/install</c> is rejected with
/// <c>409 Conflict</c> once the system is already installed.
/// </summary>
[AllowAnonymous]
[Route("api/v{version:apiVersion}/install")]
[ApiVersion("1.0")]
[ApiController]
[Produces("application/json")]
public sealed class InstallController : ApiControllerBase
{
    // ── GET /api/v1/install/status ────────────────────────────────────────

    /// <summary>Returns the current installation status of the system.</summary>
    [HttpGet("status")]
    [ProducesResponseType(typeof(InstallStatusDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Status(CancellationToken cancellationToken = default)
    {
var result = await Sender.Send(new GetInstallStatusQuery(), cancellationToken);
        return OkOrProblem(result);
    }

    // ── POST /api/v1/install ──────────────────────────────────────────────

    /// <summary>
    /// Performs the first-run installation.
    ///
    /// Creates the first tenant, the default site, and the initial admin user with the
    /// provided password. Returns <c>409 Conflict</c> if the system is already installed.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(InstallResultDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Install(
 [FromBody] InstallSystemCommand command,
    CancellationToken cancellationToken = default)
    {
        var result = await Sender.Send(command, cancellationToken);

        if (!result.IsSuccess)
            return OkOrProblem(result);

   return StatusCode(StatusCodes.Status201Created, result.Value);
  }
}
