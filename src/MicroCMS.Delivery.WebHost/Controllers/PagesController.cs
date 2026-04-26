using MicroCMS.Application.Features.Delivery.Dtos;
using MicroCMS.Application.Features.Delivery.Queries;
using MicroCMS.Delivery.Core.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MicroCMS.Delivery.WebHost.Controllers;

/// <summary>
/// Delivery API — published pages.
///
/// Returns the page tree and individual page nodes for a site.
/// No entry data is embedded; use the Entries controller to fetch linked content.
/// </summary>
[Authorize(Policy = DeliveryPolicies.ApiKeyAuthenticated)]
public sealed class PagesController : DeliveryControllerBase
{
    /// <summary>Returns the full page tree for a site, ordered by depth then title.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<DeliveryPageDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> List(
     [FromQuery] Guid siteId,
        CancellationToken cancellationToken = default)
    {
        var result = await Sender.Send(new ListPublishedPagesQuery(siteId), cancellationToken);
    return OkOrProblem(result);
    }

    /// <summary>Returns a single page by its slug.</summary>
    [HttpGet("{slug}")]
    [ProducesResponseType(typeof(DeliveryPageDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(
[FromQuery] Guid siteId,
        string slug,
  CancellationToken cancellationToken = default)
    {
   var result = await Sender.Send(new GetPublishedPageBySlugQuery(siteId, slug), cancellationToken);
        return OkOrProblem(result);
    }

    /// <summary>
    /// Renders a page server-side by walking its PageTemplate → ComponentPlacements
    /// → ComponentRenderer → Layout shell.
    ///
    /// <b>Accept: text/html</b> — returns a full HTML document (requires a Layout to be configured).
    /// <b>Accept: application/json</b> — returns <see cref="RenderedPageDto"/> with per-zone HTML fragments.
    ///
    /// If the page has no PageTemplate or no Layout the response still succeeds:
    ///   • No template → empty zones, no HTML wrapping.
    ///   • No layout → zones returned as JSON regardless of Accept header.
    /// </summary>
    [HttpGet("{slug}/render")]
    [ProducesResponseType(typeof(RenderedPageDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Render(
        [FromQuery] Guid siteId,
     string slug,
        [FromQuery] string locale = "en",
CancellationToken cancellationToken = default)
    {
        var result = await Sender.Send(
        new RenderPageBySlugQuery(siteId, slug, locale),
        cancellationToken);

if (!result.IsSuccess)
     return OkOrProblem(result);

 var rendered = result.Value;

  // If caller wants HTML and we have a full document, return it directly.
       var acceptHtml = HttpContext.Request.Headers.Accept
    .Any(h => h != null && h.Contains("text/html", StringComparison.OrdinalIgnoreCase));

        if (acceptHtml && rendered.Html is not null)
      return Content(rendered.Html, "text/html; charset=utf-8");

     // Fallback — JSON (headless or no layout configured)
return Ok(rendered);
    }
}
