using MicroCMS.Admin.Mvc.Infrastructure;
using MicroCMS.Admin.Mvc.Models.ApiDtos;
using MicroCMS.Admin.Mvc.Services.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace MicroCMS.Admin.Mvc.Controllers;

/// <summary>
/// Manages categories and tags for content taxonomy.
/// </summary>
public sealed class TaxonomyController : BaseAdminController
{
    private readonly ITaxonomyService _taxonomyService;
    private readonly ILogger<TaxonomyController> _logger;

    public TaxonomyController(ITaxonomyService taxonomyService, ILogger<TaxonomyController> logger)
    {
        _taxonomyService = taxonomyService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        try
        {
            var categoriesTask = _taxonomyService.ListCategoriesAsync(ct);
            var tagsTask = _taxonomyService.ListTagsAsync(ct: ct);

            await Task.WhenAll(categoriesTask, tagsTask);

            ViewBag.Categories = categoriesTask.Result;
            ViewBag.Tags = tagsTask.Result.Items;

            return View();
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "Failed to load taxonomy.");
            AddError("Unable to load taxonomy data.");
            ViewBag.Categories = new List<CategoryDto>();
            ViewBag.Tags = new List<TagDto>();
            return View();
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateCategory(string name, string slug, string? parentId, CancellationToken ct)
    {
        try
        {
            await _taxonomyService.CreateCategoryAsync(new CreateCategoryRequest { Name = name, Slug = slug, ParentId = parentId }, ct);
            AddSuccess($"Category '{name}' created.");
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "Failed to create category '{Name}'.", name);
            AddError(ex.Message);
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteCategory(string id, CancellationToken ct)
    {
        try
        {
            await _taxonomyService.DeleteCategoryAsync(id, ct);
            AddSuccess("Category deleted.");
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "Failed to delete category {Id}.", id);
            AddError(ex.Message);
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateTag(string name, string slug, CancellationToken ct)
    {
        try
        {
            await _taxonomyService.CreateTagAsync(new CreateTagRequest { Name = name, Slug = slug }, ct);
            AddSuccess($"Tag '{name}' created.");
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "Failed to create tag '{Name}'.", name);
            AddError(ex.Message);
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteTag(string id, CancellationToken ct)
    {
        try
        {
            await _taxonomyService.DeleteTagAsync(id, ct);
            AddSuccess("Tag deleted.");
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "Failed to delete tag {Id}.", id);
            AddError(ex.Message);
        }

        return RedirectToAction(nameof(Index));
    }
}
