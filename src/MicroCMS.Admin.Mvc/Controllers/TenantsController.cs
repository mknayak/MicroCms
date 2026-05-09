using MicroCMS.Admin.Mvc.Infrastructure;
using MicroCMS.Admin.Mvc.Models.ApiDtos;
using MicroCMS.Admin.Mvc.Models.ViewModels.Tenants;
using MicroCMS.Admin.Mvc.Services.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MicroCMS.Admin.Mvc.Controllers;

/// <summary>
/// System-level tenant management — restricted to SystemAdmin only.
/// </summary>
[Authorize(Roles = "SystemAdmin")]
public sealed class TenantsController : BaseAdminController
{
    private readonly ITenantService _tenantService;
    private readonly ILogger<TenantsController> _logger;

    public TenantsController(ITenantService tenantService, ILogger<TenantsController> logger)
    {
        _tenantService = tenantService;
        _logger = logger;
    }

    // ── Index ────────────────────────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> Index(int page = 1, CancellationToken ct = default)
    {
        try
        {
            var result = await _tenantService.ListAsync(page, pageSize: 20, ct);
            var model = new TenantsViewModel
            {
                Items = result.Items,
                TotalCount = result.TotalCount,
                PageNumber = result.PageNumber,
                TotalPages = result.TotalPages,
            };
            return View(model);
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "Failed to list tenants.");
            AddError("Unable to load tenants.");
            return View(new TenantsViewModel());
        }
    }

    // ── Detail ───────────────────────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> Detail(string id, CancellationToken ct)
    {
        try
        {
            var tenant = await _tenantService.GetByIdAsync(id, ct);
            return View(new TenantDetailViewModel { Tenant = tenant });
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "Failed to load tenant {Id}.", id);
            AddError("Tenant not found.");
            return RedirectToAction(nameof(Index));
        }
    }

    // ── Update settings ──────────────────────────────────────────────────────

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateSettings(
        string id,
        UpdateTenantSettingsRequest model,
        CancellationToken ct)
    {
        try
        {
            await _tenantService.UpdateAsync(id, model, ct);
            AddSuccess("Tenant settings updated.");
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "Failed to update settings for tenant {Id}.", id);
            AddError(ex.Message);
        }

        return RedirectToAction(nameof(Detail), new { id });
    }

    // ── Create site ──────────────────────────────────────────────────────────

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateSite(
        string id,
        CreateSiteRequest model,
        CancellationToken ct)
    {
        try
        {
            await _tenantService.CreateSiteAsync(id, model, ct);
            AddSuccess($"Site '{model.Name}' created.");
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "Failed to create site for tenant {Id}.", id);
            AddError(ex.Message);
        }

        return RedirectToAction(nameof(Detail), new { id });
    }

    // ── Onboard ──────────────────────────────────────────────────────────────

    [HttpGet]
    public IActionResult Onboard() => View(new OnboardTenantRequest());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Onboard(OnboardTenantRequest model, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return View(model);

        try
        {
            var tenant = await _tenantService.OnboardAsync(model, ct);
            AddSuccess($"Tenant '{tenant.DisplayName}' onboarded successfully.");
            return RedirectToAction(nameof(Detail), new { id = tenant.Id });
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "Failed to onboard tenant '{Slug}'.", model.Slug);
            HandleApiError(ex);
            return View(model);
        }
    }
}
