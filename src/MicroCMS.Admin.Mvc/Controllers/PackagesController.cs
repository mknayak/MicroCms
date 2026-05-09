using MicroCMS.Admin.Mvc.Infrastructure;
using MicroCMS.Admin.Mvc.Services.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace MicroCMS.Admin.Mvc.Controllers;

/// <summary>
/// Package export and import operations.
/// Restricted to SystemAdmin, TenantAdmin, and Designer roles.
/// </summary>
[Authorize(Roles = "SystemAdmin,TenantAdmin,Designer")]
public sealed class PackagesController : BaseAdminController
{
    private readonly IPackageService _packageService;
    private readonly ILogger<PackagesController> _logger;

    public PackagesController(IPackageService packageService, ILogger<PackagesController> logger)
    {
        _packageService = packageService;
        _logger = logger;
    }

    [HttpGet]
    public IActionResult Export() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Export(string? siteId, CancellationToken ct)
    {
        var tenantId = GetTenantId();
        if (string.IsNullOrWhiteSpace(tenantId))
        {
            AddError("Unable to determine tenant. Please re-login.");
            return View();
        }

        try
        {
            var bytes = await _packageService.ExportAsync(tenantId, siteId, ct);
            var fileName = $"microcms-export-{DateTime.UtcNow:yyyyMMdd-HHmmss}.zip";
            return File(bytes, "application/zip", fileName);
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "Package export failed.");
            AddError($"Export failed: {ex.Message}");
            return View();
        }
    }

    [HttpGet]
    public IActionResult Import() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Import(IFormFile packageFile, CancellationToken ct)
    {
        if (packageFile is null || packageFile.Length == 0)
        {
            AddError("Please select a package file to import.");
            return View();
        }

        try
        {
            await using var stream = packageFile.OpenReadStream();
            await _packageService.ImportAsync(stream, packageFile.FileName, ct);
            TempData["ImportResult"] = "success";
            AddSuccess("Package imported successfully.");
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "Package import failed.");
            AddError($"Import failed: {ex.Message}");
        }

        return RedirectToAction(nameof(Import), new { step = 3 });
    }

    private string? GetTenantId()
    {
        // The tenant ID may be stored as a claim when the JWT is decoded;
        // for now we extract it from available identity claims.
        return User.FindFirst("tenantId")?.Value
            ?? User.FindFirst(ClaimTypes.GroupSid)?.Value;
    }
}
