using MicroCMS.Application.Features.Delivery.Dtos;
using MicroCMS.Application.Features.Delivery.Queries;
using MicroCMS.Delivery.Core.Extensions;
using MicroCMS.Delivery.Core.Rendering;
using MicroCMS.Domain.Aggregates.Components;
using MicroCMS.Domain.Repositories;
using MicroCMS.Shared.Ids;
using MicroCMS.Shared.Primitives;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MicroCMS.Delivery.WebHost.Controllers;

/// <summary>
/// Delivery API — published component items.
///
/// Supports JSON data delivery as well as server-side rendered HTML
/// via the optional <c>Accept: text/html</c> header (Handlebars templates only).
/// </summary>
[Authorize(Policy = DeliveryPolicies.ApiKeyAuthenticated)]
public sealed class ComponentsController : DeliveryControllerBase
{
    /// <summary>
    /// Lists published component items for a given component key.
    /// </summary>
    [HttpGet("{componentKey}")]
    [ProducesResponseType(typeof(PagedList<DeliveryComponentItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> List(
          [FromQuery] Guid siteId,
        string componentKey,
          [FromQuery] int page = 1,
          [FromQuery] int pageSize = 50,
       CancellationToken cancellationToken = default)
    {
        pageSize = Math.Clamp(pageSize, 1, 200);
        var result = await Sender.Send(
          new ListPublishedComponentItemsQuery(siteId, componentKey, page, pageSize),
            cancellationToken);
        return OkOrProblem(result);
    }

    /// <summary>
    /// Returns a single published component item by ID.
    /// When the caller sends <c>Accept: text/html</c> and the component has a Handlebars
    /// template, the response is server-side rendered HTML instead of JSON.
    /// </summary>
    [HttpGet("{componentKey}/{itemId:guid}")]
    [ProducesResponseType(typeof(DeliveryComponentItemDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(
        [FromQuery] Guid siteId,
        string componentKey,
 Guid itemId,
     CancellationToken cancellationToken = default)
    {
        var result = await Sender.Send(
          new GetPublishedComponentItemQuery(siteId, itemId),
     cancellationToken);

        if (!result.IsSuccess)
            return OkOrProblem(result);

        // ── Optional HTML rendering ───────────────────────────────────────
        var acceptHtml = Request.Headers.Accept.ToString()
               .Contains("text/html", StringComparison.OrdinalIgnoreCase);

        if (acceptHtml)
        {
            var renderer = HttpContext.RequestServices
           .GetRequiredService<IComponentRenderer>();
            var compRepo = HttpContext.RequestServices
        .GetRequiredService<IRepository<Component, ComponentId>>();

            var comp = await compRepo.GetByIdAsync(
              new ComponentId(result.Value.ComponentId), cancellationToken);

            if (comp is not null &&
        comp.TemplateType == RenderingTemplateType.Handlebars &&
         !string.IsNullOrWhiteSpace(comp.TemplateContent))
            {
                var html = await renderer.RenderAsync(comp, result.Value, cancellationToken);
                return Content(html, "text/html");
            }
        }

        return Ok(result.Value);
    }
}
