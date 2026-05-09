using MicroCMS.Admin.Mvc.Infrastructure;
using MicroCMS.Admin.Mvc.Models.ViewModels.Search;
using MicroCMS.Admin.Mvc.Services.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace MicroCMS.Admin.Mvc.Controllers;

/// <summary>
/// Full-text search across entries.
/// </summary>
public sealed class SearchController : BaseAdminController
{
    private readonly ISearchService _searchService;
    private readonly ILogger<SearchController> _logger;

    public SearchController(ISearchService searchService, ILogger<SearchController> logger)
    {
        _searchService = searchService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? q, int page = 1, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(q))
        {
            return View(new SearchViewModel());
        }

        try
        {
            var results = await _searchService.SearchAsync(q, page, pageSize: 20, ct);
            var model = new SearchViewModel
            {
                Query = q,
                Hits = results.Hits,
                TotalCount = results.TotalCount,
                Page = results.Page,
                PageSize = results.PageSize,
            };
            return View(model);
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "Search failed for query '{Query}'.", q);
            AddError("Search is currently unavailable.");
            return View(new SearchViewModel { Query = q });
        }
    }
}
