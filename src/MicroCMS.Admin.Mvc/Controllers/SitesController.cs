using MicroCMS.Admin.Mvc.Infrastructure;
using MicroCMS.Admin.Mvc.Models.ApiDtos;
using MicroCMS.Admin.Mvc.Services.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MicroCMS.Admin.Mvc.Controllers;

/// <summary>
/// Per-site settings management.
/// </summary>
[Authorize(Roles = "SystemAdmin,TenantAdmin,SiteAdmin")]
public sealed class SitesController : BaseAdminController
{
    private readonly ISiteService _siteService;
    private readonly ILogger<SitesController> _logger;

    public SitesController(ISiteService siteService, ILogger<SitesController> logger)
    {
        _siteService = siteService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Settings(string id, CancellationToken ct)
    {
        try
        {
            var siteTask = _siteService.GetByIdAsync(id, ct);
            var settingsTask = _siteService.GetSettingsAsync(id, ct);
            await Task.WhenAll(siteTask, settingsTask);

            ViewBag.Site = siteTask.Result;
            return View(settingsTask.Result);
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "Failed to load settings for site {Id}.", id);
            AddError("Site not found.");
            return RedirectToAction("Index", "Dashboard");
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Settings(string id, UpdateSiteSettingsRequest model, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Site = await _siteService.GetByIdAsync(id, ct);
            return View(model);
        }

        try
        {
            await _siteService.UpdateSettingsAsync(id, model, ct);
            AddSuccess("Site settings saved.");
            return RedirectToAction(nameof(Settings), new { id });
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "Failed to update settings for site {Id}.", id);
            HandleApiError(ex);
            ViewBag.Site = await _siteService.GetByIdAsync(id, ct);
            return View(model);
        }
    }
}
