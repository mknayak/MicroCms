using MicroCMS.Application.Features.Tenants.Commands;
using MicroCMS.Application.Features.Tenants.Dtos;
using MicroCMS.Application.Features.Tenants.Queries;
using MicroCMS.Shared.Primitives;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MicroCMS.Api.Controllers;

/// <summary>Tenant CRUD and site management.</summary>
[Authorize]
public sealed class TenantsController : ApiControllerBase
{
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

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(TenantDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken = default)
    {
      var result = await Sender.Send(new GetTenantQuery(id), cancellationToken);
        return OkOrProblem(result);
    }

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
   return result.IsSuccess ? StatusCode(StatusCodes.Status201Created, result.Value) : OkOrProblem(result);
  }
}

public sealed record UpdateTenantSettingsRequest(
string DisplayName, string DefaultLocale, string TimeZoneId, bool AiEnabled, string? LogoUrl);

public sealed record AddSiteRequest(string Name, string Handle, string DefaultLocale);
