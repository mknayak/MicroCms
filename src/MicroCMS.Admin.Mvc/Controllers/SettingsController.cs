using System.Security.Claims;
using MicroCMS.Admin.Mvc.Infrastructure;
using MicroCMS.Admin.Mvc.Models.ApiDtos;
using MicroCMS.Admin.Mvc.Services.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MicroCMS.Admin.Mvc.Controllers;

/// <summary>
/// Organisation / tenant settings page.
/// </summary>
[Authorize(Roles = "SystemAdmin,TenantAdmin")]
public sealed class SettingsController : BaseAdminController
{
    private readonly ITenantService _tenantService;
    private readonly ILogger<SettingsController> _logger;

    public SettingsController(ITenantService tenantService, ILogger<SettingsController> logger)
    {
        _tenantService = tenantService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var tenantId = GetCurrentTenantId();
        if (string.IsNullOrWhiteSpace(tenantId))
            return View(new TenantDetail());

        try
        {
            var tenant = await _tenantService.GetByIdAsync(tenantId, ct);
            return View(tenant);
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "Failed to load settings for tenant {TenantId}.", tenantId);
            AddError("Unable to load organisation settings.");
            return View(new TenantDetail());
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(UpdateTenantSettingsRequest model, CancellationToken ct)
    {
        var tenantId = GetCurrentTenantId();
        if (string.IsNullOrWhiteSpace(tenantId))
        {
            AddError("Cannot determine your organisation. Please re-login.");
            return View(new TenantDetail());
        }

        if (!ModelState.IsValid)
        {
            var tenant = await SafeLoadTenant(tenantId, ct);
            return View(tenant);
        }

        try
        {
            await _tenantService.UpdateAsync(tenantId, model, ct);
            AddSuccess("Organisation settings saved.");
            return RedirectToAction(nameof(Index));
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "Failed to update settings for tenant {TenantId}.", tenantId);
            HandleApiError(ex);
            var tenant = await SafeLoadTenant(tenantId, ct);
            return View(tenant);
        }
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private string? GetCurrentTenantId() =>
        User.FindFirst("tenantId")?.Value
        ?? User.FindFirst(ClaimTypes.GroupSid)?.Value;

    private async Task<TenantDetail> SafeLoadTenant(string tenantId, CancellationToken ct)
    {
        try { return await _tenantService.GetByIdAsync(tenantId, ct); }
        catch { return new TenantDetail(); }
    }
}
