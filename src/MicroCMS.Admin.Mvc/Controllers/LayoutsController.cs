using MicroCMS.Admin.Mvc.Infrastructure;
using MicroCMS.Admin.Mvc.Models.ApiDtos;
using MicroCMS.Admin.Mvc.Services.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MicroCMS.Admin.Mvc.Controllers;

/// <summary>
/// Manages site layouts: list, create, edit metadata, and delete.
/// </summary>
[Authorize(Roles = "SystemAdmin,TenantAdmin,Designer,Editor")]
public sealed class LayoutsController : BaseAdminController
{
    private readonly ILayoutService _layoutService;
    private readonly ILogger<LayoutsController> _logger;

    public LayoutsController(ILayoutService layoutService, ILogger<LayoutsController> logger)
    {
        _layoutService = layoutService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        try
        {
            var layouts = await _layoutService.ListAsync(ct);
            return View(layouts);
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "Failed to list layouts.");
            AddError("Unable to load layouts.");
            return View(new List<LayoutListItem>());
        }
    }

    [HttpGet]
    public IActionResult Create() => View(new CreateLayoutRequest());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateLayoutRequest model, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            var layout = await _layoutService.CreateAsync(model, ct);
            AddSuccess($"Layout '{layout.Name}' created.");
            return RedirectToAction(nameof(Index));
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "Failed to create layout '{Name}'.", model.Name);
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
            await _layoutService.DeleteAsync(id, ct);
            AddSuccess("Layout deleted.");
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "Failed to delete layout {Id}.", id);
            AddError(ex.Message);
        }

        return RedirectToAction(nameof(Index));
    }
}
