using MicroCMS.Application.Features.Delivery.Dtos;
using MicroCMS.Application.Features.Delivery.Queries;
using MicroCMS.Delivery.Core.Extensions;
using MicroCMS.Shared.Primitives;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MicroCMS.Delivery.WebHost.Controllers;

/// <summary>
/// Delivery API — published entries.
///
/// All endpoints require a valid API key in the <c>X-Api-Key</c> header.
/// Only <b>Published</b> entries are returned; draft/pending/archived entries are hidden.
/// </summary>
[Authorize(Policy = DeliveryPolicies.ApiKeyAuthenticated)]
public sealed class EntriesController : DeliveryControllerBase
{
    /// <summary>
    /// Lists published entries for a content type.
    /// </summary>
    /// <param name="siteId">The site GUID.</param>
    /// <param name="contentTypeKey">The content type handle, e.g. <c>blog_post</c>.</param>
    /// <param name="locale">Optional BCP-47 locale filter, e.g. <c>en</c> or <c>fr-CA</c>.</param>
    /// <param name="page">1-based page number.</param>
    /// <param name="pageSize">Results per page (max 200).</param>
    [HttpGet("{contentTypeKey}")]
    [ProducesResponseType(typeof(PagedList<DeliveryEntryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> List(
        [FromQuery] Guid siteId,
 string contentTypeKey,
        [FromQuery] string? locale = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
    CancellationToken cancellationToken = default)
    {
    pageSize = Math.Clamp(pageSize, 1, 200);
        var result = await Sender.Send(
   new ListPublishedEntriesQuery(siteId, contentTypeKey, locale, page, pageSize),
      cancellationToken);
        return OkOrProblem(result);
    }

    /// <summary>
    /// Returns a single published entry by its slug.
    /// </summary>
    /// <param name="siteId">The site GUID.</param>
    /// <param name="contentTypeKey">The content type handle.</param>
    /// <param name="slug">The entry slug.</param>
    /// <param name="locale">BCP-47 locale (default: <c>en</c>).</param>
[HttpGet("{contentTypeKey}/{slug}")]
    [ProducesResponseType(typeof(DeliveryEntryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(
        [FromQuery] Guid siteId,
 string contentTypeKey,
        string slug,
        [FromQuery] string locale = "en",
        CancellationToken cancellationToken = default)
    {
        var result = await Sender.Send(
            new GetPublishedEntryBySlugQuery(siteId, contentTypeKey, slug, locale),
            cancellationToken);
        return OkOrProblem(result);
    }
}
