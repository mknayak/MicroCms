using MicroCMS.Application.Common.Interfaces;
using MicroCMS.Application.Features.Search.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MicroCMS.Api.Controllers;

/// <summary>
/// Sprint 9 — full-text search endpoint.
/// Searches are tenant-scoped server-side; callers cannot query another tenant's content.
/// </summary>
[Authorize]
public sealed class SearchController : ApiControllerBase
{
    /// <summary>Executes a full-text search across the current tenant's published entries.</summary>
  [HttpGet]
    [ProducesResponseType(typeof(SearchResults), StatusCodes.Status200OK)]
    public async Task<IActionResult> Search(
        [FromQuery] string q,
        [FromQuery] Guid? siteId = null,
[FromQuery] Guid? contentTypeId = null,
      [FromQuery] string? locale = null,
    [FromQuery] string? status = "Published",
    [FromQuery] int page = 1,
     [FromQuery] int pageSize = 20,
     CancellationToken cancellationToken = default)
    {
 var query = new SearchEntriesQuery(
   Query: q ?? string.Empty,
            SiteId: siteId,
      ContentTypeId: contentTypeId,
     Locale: locale,
     Status: status,
          Page: page,
     PageSize: pageSize);

    var result = await Sender.Send(query, cancellationToken);
        return OkOrProblem(result);
  }
}
