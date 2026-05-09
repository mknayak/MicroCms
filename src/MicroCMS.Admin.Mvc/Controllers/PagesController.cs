using MicroCMS.Admin.Mvc.Infrastructure;
using MicroCMS.Admin.Mvc.Models.ApiDtos;
using MicroCMS.Admin.Mvc.Services.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MicroCMS.Admin.Mvc.Controllers;

/// <summary>
/// Manages site pages: view page tree, create pages, and delete.
/// </summary>
[Authorize(Roles = "SystemAdmin,TenantAdmin,Editor")]
public sealed class PagesController : BaseAdminController
{
    private readonly IPageService _pageService;
    private readonly ILogger<PagesController> _logger;

    public PagesController(IPageService pageService, ILogger<PagesController> logger)
    {
        _pageService = pageService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        try
        {
            var tree = await _pageService.GetTreeAsync(ct);
            return View(tree);
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "Failed to load page tree.");
            AddError("Unable to load pages.");
            return View(new List<PageTreeNode>());
        }
    }

    [HttpGet]
    public IActionResult Create() => View(new CreateStaticPageRequest());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateStaticPageRequest model, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            var page = await _pageService.CreateStaticPageAsync(model, ct);
            AddSuccess($"Page '{page.Title}' created.");
            return RedirectToAction(nameof(Index));
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "Failed to create page '{Title}'.", model.Title);
            HandleApiError(ex);
            return View(model);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string id, CancellationToken ct)
    {
        try
        {
            await _pageService.DeleteAsync(id, ct);
            AddSuccess("Page deleted.");
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "Failed to delete page {Id}.", id);
            AddError(ex.Message);
        }

        return RedirectToAction(nameof(Index));
    }
}
