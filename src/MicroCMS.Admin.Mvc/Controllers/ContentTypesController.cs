using MicroCMS.Admin.Mvc.Infrastructure;
using MicroCMS.Admin.Mvc.Models.ApiDtos;
using MicroCMS.Admin.Mvc.Models.ViewModels.ContentTypes;
using MicroCMS.Admin.Mvc.Services.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace MicroCMS.Admin.Mvc.Controllers;

/// <summary>
/// CRUD operations for content types.
/// </summary>
public sealed class ContentTypesController : BaseAdminController
{
    private readonly IContentTypeService _contentTypeService;
    private readonly IEntryService _entryService;
    private readonly ILogger<ContentTypesController> _logger;

    public ContentTypesController(
        IContentTypeService contentTypeService,
        IEntryService entryService,
        ILogger<ContentTypesController> logger)
    {
        _contentTypeService = contentTypeService;
        _entryService = entryService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index(int page = 1, CancellationToken ct = default)
    {
        try
        {
            var result = await _contentTypeService.ListAsync(page, pageSize: 20, ct);
            var model = new ContentTypeListViewModel
            {
                Items = result.Items,
                TotalCount = result.TotalCount,
                PageNumber = result.PageNumber,
                PageSize = result.PageSize,
                TotalPages = result.TotalPages,
            };
            return View(model);
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "Failed to list content types.");
            AddError("Unable to load content types.");
            return View(new ContentTypeListViewModel());
        }
    }

    [HttpGet]
    public async Task<IActionResult> Detail(string id, CancellationToken ct = default)
    {
        try
        {
            var ct2 = ct;
            var contentType = await _contentTypeService.GetByIdAsync(id, ct2);
            var entries = await _entryService.ListAsync(
                new EntryListParams { ContentTypeId = id, PageNumber = 1, PageSize = 10 }, ct2);

            var model = new ContentTypeDetailViewModel
            {
                ContentType = contentType,
                RecentEntries = entries.Items,
                EntryCount = entries.TotalCount,
            };
            return View(model);
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "Failed to load content type {Id}.", id);
            AddError("Content type not found.");
            return RedirectToAction(nameof(Index));
        }
    }

    [HttpGet]
    public IActionResult Create() => View(new ContentTypeFormViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ContentTypeFormViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            var request = new CreateContentTypeRequest
            {
                Handle = model.Handle,
                DisplayName = model.DisplayName,
                Description = model.Description,
                LocalizationMode = model.LocalizationMode,
                Kind = model.Kind,
            };

            var created = await _contentTypeService.CreateAsync(request, ct);
            AddSuccess($"Content type '{created.DisplayName}' created successfully.");
            return RedirectToAction(nameof(Detail), new { id = created.Id });
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "Failed to create content type.");
            HandleApiError(ex);
            return View(model);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Edit(string id, CancellationToken ct)
    {
        try
        {
            var contentType = await _contentTypeService.GetByIdAsync(id, ct);
            var model = new ContentTypeFormViewModel
            {
                Id = contentType.Id,
                Handle = contentType.Handle,
                DisplayName = contentType.DisplayName,
                Description = contentType.Description,
                LocalizationMode = contentType.LocalizationMode,
                Kind = contentType.Kind,
                Fields = contentType.Fields,
            };
            return View(model);
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "Failed to load content type {Id} for editing.", id);
            AddError("Content type not found.");
            return RedirectToAction(nameof(Index));
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string id, ContentTypeFormViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            // Re-load fields for the schema tab
            try
            {
                var ct2 = await _contentTypeService.GetByIdAsync(id, ct);
                model.Fields = ct2.Fields;
            }
            catch { /* swallow — form will re-render without field list */ }
            return View(model);
        }

        try
        {
            var request = new UpdateContentTypeRequest
            {
                DisplayName = model.DisplayName,
                Description = model.Description,
                LocalizationMode = model.LocalizationMode,
            };

            await _contentTypeService.UpdateAsync(id, request, ct);
            AddSuccess("Content type updated successfully.");
            return RedirectToAction(nameof(Edit), new { id });
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "Failed to update content type {Id}.", id);
            HandleApiError(ex);
            return View(model);
        }
    }

    // ── Field management ─────────────────────────────────────────────────────

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddField(string id, AddFieldRequest model, string? optionsRaw, CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(optionsRaw))
            model.Options = ParseOptions(optionsRaw);

        try
        {
            await _contentTypeService.AddFieldAsync(id, model, ct);
            AddSuccess($"Field '{model.Label}' added.");
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "Failed to add field to content type {Id}.", id);
            AddError(ex.Message);
        }

        return RedirectToAction(nameof(Edit), new { id, tab = "schema" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateField(string id, string fieldId, UpdateFieldRequest model, string? optionsRaw, CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(optionsRaw))
            model.Options = ParseOptions(optionsRaw);

        try
        {
            await _contentTypeService.UpdateFieldAsync(id, fieldId, model, ct);
            AddSuccess($"Field '{model.Label}' updated.");
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "Failed to update field {FieldId} on content type {Id}.", fieldId, id);
            AddError(ex.Message);
        }

        return RedirectToAction(nameof(Edit), new { id, tab = "schema" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteField(string id, string fieldId, CancellationToken ct)
    {
        try
        {
            await _contentTypeService.DeleteFieldAsync(id, fieldId, ct);
            AddSuccess("Field deleted.");
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "Failed to delete field {FieldId} from content type {Id}.", fieldId, id);
            AddError(ex.Message);
        }

        return RedirectToAction(nameof(Edit), new { id, tab = "schema" });
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static List<string> ParseOptions(string raw) =>
        raw.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
           .Where(s => !string.IsNullOrWhiteSpace(s))
           .ToList();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string id, CancellationToken ct)
    {
        try
        {
            await _contentTypeService.DeleteAsync(id, ct);
            AddSuccess("Content type deleted.");
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "Failed to delete content type {Id}.", id);
            AddError(ex.Message);
        }

        return RedirectToAction(nameof(Index));
    }
}
