using Asp.Versioning;
using MicroCMS.Application.Features.Tenants.Commands;
using MicroCMS.Application.Features.Tenants.Dtos;
using MicroCMS.Application.Features.Tenants.Queries;
using MicroCMS.Application.Services;
using MicroCMS.Shared.Primitives;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MicroCMS.Api.Controllers;

/// <summary>
/// Tenant admin API — system-level operations requiring <c>SystemAdmin</c> role.
/// Distinct from <see cref="TenantsController"/> (tenant-scoped self-service).
/// Route: /api/v1/admin/tenants
/// </summary>
[Authorize]
[Route("api/v{version:apiVersion}/admin/tenants")]
[ApiVersion("1.0")]
[ApiController]
[Produces("application/json")]
public sealed class TenantAdminController : ApiControllerBase
{
    /// <summary>Lists all tenants across the system (SystemAdmin only).</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedList<TenantListItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(
    [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await Sender.Send(new ListTenantsQuery(page, pageSize), cancellationToken);
        return OkOrProblem(result);
    }

    /// <summary>Gets the current authenticated user's own tenant.</summary>
    [HttpGet("current")]
    [ProducesResponseType(typeof(TenantDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCurrent(CancellationToken cancellationToken = default)
    {
        var result = await Sender.Send(new GetCurrentTenantQuery(), cancellationToken);
        return OkOrProblem(result);
    }

    /// <summary>Gets a tenant by ID (SystemAdmin only).</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(TenantDto), StatusCodes.Status200OK)]
 [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken = default)
    {
var result = await Sender.Send(new GetTenantQuery(id), cancellationToken);
        return OkOrProblem(result);
    }

    /// <summary>
    /// Full atomic tenant provisioning: tenant + default site + admin user.
    /// </summary>
    [HttpPost("onboard")]
    [ProducesResponseType(typeof(TenantOnboardingResult), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
[ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Onboard(
        [FromBody] OnboardTenantCommand command,
  CancellationToken cancellationToken = default)
    {
var result = await Sender.Send(command, cancellationToken);
      return CreatedOrProblem(result, nameof(Get), new { id = result.IsSuccess ? result.Value.TenantId : Guid.Empty });
    }

    /// <summary>Creates a bare tenant record (without onboarding). Use /onboard for full provisioning.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(TenantDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Create(
        [FromBody] CreateTenantCommand command,
        CancellationToken cancellationToken = default)
    {
        var result = await Sender.Send(command, cancellationToken);
     return CreatedOrProblem(result, nameof(Get), new { id = result.IsSuccess ? result.Value.Id : Guid.Empty });
    }

    /// <summary>Updates tenant settings.</summary>
    [HttpPut("{id:guid}/settings")]
    [ProducesResponseType(typeof(TenantDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateSettings(
        Guid id,
        [FromBody] UpdateTenantSettingsRequest request,
        CancellationToken cancellationToken = default)
    {
      var result = await Sender.Send(
    new UpdateTenantSettingsCommand(id, request.DisplayName, request.DefaultLocale,
            request.TimeZoneId, request.AiEnabled, request.LogoUrl),
          cancellationToken);
      return OkOrProblem(result);
    }

    /// <summary>Adds a site to a tenant.</summary>
    [HttpPost("{id:guid}/sites")]
    [ProducesResponseType(typeof(SiteDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddSite(
    Guid id,
     [FromBody] AddSiteRequest request,
        CancellationToken cancellationToken = default)
    {
 var result = await Sender.Send(
      new AddSiteCommand(id, request.Name, request.Handle, request.DefaultLocale),
            cancellationToken);
 return result.IsSuccess
            ? StatusCode(StatusCodes.Status201Created, result.Value)
         : OkOrProblem(result);
    }
}
