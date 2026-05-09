using MicroCMS.Admin.Mvc.Infrastructure;
using MicroCMS.Admin.Mvc.Models.ApiDtos;
using MicroCMS.Admin.Mvc.Services.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace MicroCMS.Admin.Mvc.Controllers;

/// <summary>
/// Manages the component library and component items.
/// </summary>
public sealed class ComponentsController : BaseAdminController
{
    private readonly IComponentService _componentService;
    private readonly ILogger<ComponentsController> _logger;

    public ComponentsController(IComponentService componentService, ILogger<ComponentsController> logger)
    {
        _componentService = componentService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index(int page = 1, CancellationToken ct = default)
    {
        try
        {
            var result = await _componentService.ListAsync(page, pageSize: 20, ct);
            ViewBag.TotalPages = result.TotalPages;
            ViewBag.PageNumber = result.PageNumber;
            return View(result.Items);
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "Failed to list components.");
            AddError("Unable to load component library.");
            return View(new List<ComponentListItem>());
        }
    }

    [HttpGet]
    public async Task<IActionResult> Detail(string id, CancellationToken ct)
    {
        try
        {
            var component = await _componentService.GetByIdAsync(id, ct);
            return View(component);
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "Failed to load component {Id}.", id);
            AddError("Component not found.");
            return RedirectToAction(nameof(Index));
        }
    }

    [HttpGet]
    public async Task<IActionResult> Items(string id, int page = 1, CancellationToken ct = default)
    {
        try
        {
            var componentTask = _componentService.GetByIdAsync(id, ct);
            var itemsTask = _componentService.ListItemsAsync(id, page, pageSize: 20, ct);
            await Task.WhenAll(componentTask, itemsTask);

            ViewBag.Component = componentTask.Result;
            ViewBag.TotalPages = itemsTask.Result.TotalPages;
            ViewBag.PageNumber = itemsTask.Result.PageNumber;
            return View(itemsTask.Result.Items);
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "Failed to load items for component {Id}.", id);
            AddError("Unable to load component items.");
            return RedirectToAction(nameof(Index));
        }
    }

    [HttpGet]
    public IActionResult Create() => View(new CreateComponentRequest());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateComponentRequest model, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            var component = await _componentService.CreateAsync(model, ct);
            AddSuccess($"Component '{component.Name}' created.");
            return RedirectToAction(nameof(Detail), new { id = component.Id });
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "Failed to create component '{Name}'.", model.Name);
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
            await _componentService.DeleteAsync(id, ct);
            AddSuccess("Component deleted.");
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "Failed to delete component {Id}.", id);
            AddError(ex.Message);
        }

        return RedirectToAction(nameof(Index));
    }
}
