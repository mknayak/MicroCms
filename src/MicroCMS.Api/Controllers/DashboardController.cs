using Asp.Versioning;
using MicroCMS.Application.Features.Dashboard.Dtos;
using MicroCMS.Application.Features.Dashboard.Queries;
using MicroCMS.Shared.Primitives;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MicroCMS.Api.Controllers;

/// <summary>
/// Admin dashboard endpoints.
/// Route: /api/v1/admin/dashboard
/// </summary>
[Authorize]
[Route("api/v{version:apiVersion}/admin/dashboard")]
[ApiVersion("1.0")]
[ApiController]
public sealed class DashboardController : ApiControllerBase
{
    /// <summary>Returns aggregate statistics for the current site dashboard.</summary>
    [HttpGet("stats")]
    [ProducesResponseType(typeof(DashboardStatsDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStats(CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new GetDashboardStatsQuery(), cancellationToken);
        return OkOrProblem(result);
    }

    /// <summary>Returns a paged list of recent activity for the current site.</summary>
    [HttpGet("activity")]
    [ProducesResponseType(typeof(PagedList<DashboardActivityItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetActivity(
        [FromQuery] int pageSize = 10,
        [FromQuery] int pageNumber = 1,
        CancellationToken cancellationToken = default)
    {
        var result = await Sender.Send(new GetDashboardActivityQuery(pageSize, pageNumber), cancellationToken);
        return OkOrProblem(result);
    }
}
