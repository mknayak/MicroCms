using MicroCMS.Admin.Mvc.Infrastructure;
using MicroCMS.Admin.Mvc.Models.ApiDtos;
using MicroCMS.Admin.Mvc.Models.ViewModels.Entries;
using MicroCMS.Admin.Mvc.Services.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace MicroCMS.Admin.Mvc.Controllers;

/// <summary>
/// Manages content entries: list, create, edit, publish/unpublish, workflow, and delete.
/// </summary>
public sealed class EntriesController : BaseAdminController
{
    private readonly IEntryService _entryService;
    private readonly IContentTypeService _contentTypeService;
    private readonly ILogger<EntriesController> _logger;

    public EntriesController(
        IEntryService entryService,
        IContentTypeService contentTypeService,
        ILogger<EntriesController> logger)
    {
        _entryService = entryService;
        _contentTypeService = contentTypeService;
        _logger = logger;
    }

    // ── Index ────────────────────────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> Index(
        string? contentTypeId,
        string? status,
        string? search,
        int page = 1,
        CancellationToken ct = default)
    {
        try
        {
            var parameters = new EntryListParams
            {
                ContentTypeId = contentTypeId,
                Status = status,
                Search = search,
                PageNumber = page,
                PageSize = 20,
            };

            var result = await _entryService.ListAsync(parameters, ct);
            var model = new EntryListViewModel
            {
                Items = result.Items,
                TotalCount = result.TotalCount,
                PageNumber = result.PageNumber,
                PageSize = result.PageSize,
                TotalPages = result.TotalPages,
                ContentTypeId = contentTypeId,
                StatusFilter = status,
                SearchQuery = search,
            };

            return View(model);
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "Failed to list entries.");
            AddError("Unable to load entries.");
            return View(new EntryListViewModel());
        }
    }

    // ── Create ───────────────────────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> Create(string? contentTypeId, CancellationToken ct)
    {
        try
        {
            var ctListTask = _contentTypeService.ListAsync(1, 100, ct);

            ContentTypeDto? contentType = null;
            if (!string.IsNullOrWhiteSpace(contentTypeId))
                contentType = await _contentTypeService.GetByIdAsync(contentTypeId, ct);

            var ctList = await ctListTask;

            var model = new EntryFormViewModel
            {
                IsNew = true,
                ContentTypes = ctList.Items,
                ContentType = contentType ?? new ContentTypeDto(),
            };

            return View(model);
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "Failed to load create entry form.");
            AddError("Unable to load form.");
            return RedirectToAction(nameof(Index), new { contentTypeId });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        string? contentTypeId,
        EntryFormViewModel model,
        CancellationToken ct)
    {
        try
        {
            // Reload schema for form re-render on error
            var contentType = await _contentTypeService.GetByIdAsync(
                contentTypeId ?? model.ContentType.Id, ct);

            if (!ModelState.IsValid)
            {
                var ctList = await _contentTypeService.ListAsync(1, 100, ct);
                return View(new EntryFormViewModel
                {
                    IsNew = true,
                    ContentType = contentType,
                    ContentTypes = ctList.Items,
                    Slug = model.Slug,
                    Locale = model.Locale,
                    Fields = model.Fields,
                });
            }

            // Create the entry shell (slug + locale)
            var created = await _entryService.CreateAsync(new CreateEntryRequest
            {
                SiteId = contentType.SiteId,
                ContentTypeId = contentType.Id,
                Slug = model.Slug,
                Locale = model.Locale,
            }, ct);

            // Immediately persist field values
            var fields = BuildFieldDictionary(model.Fields, contentType.Fields);
            await _entryService.UpdateAsync(created.Id, new UpdateEntryRequest
            {
                Fields = fields,
                ChangeNote = "Initial save",
            }, ct);

            AddSuccess("Entry created successfully.");
            return RedirectToAction(nameof(Edit), new { id = created.Id });
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "Failed to create entry.");
            HandleApiError(ex);
            return View(model);
        }
    }

    // ── Edit ─────────────────────────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> Edit(string id, CancellationToken ct)
    {
        try
        {
            var entry = await _entryService.GetByIdAsync(id, ct);
            var contentType = await _contentTypeService.GetByIdAsync(entry.ContentTypeId, ct);

            var model = new EntryFormViewModel
            {
                IsNew = false,
                Existing = entry,
                ContentType = contentType,
                Slug = entry.Slug,
                Locale = entry.Locale,
            };

            return View(model);
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "Failed to load entry {Id} for editing.", id);
            AddError("Entry not found.");
            return RedirectToAction(nameof(Index));
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        string id,
        EntryFormViewModel model,
        CancellationToken ct)
    {
        try
        {
            var entry = await _entryService.GetByIdAsync(id, ct);
            var contentType = await _contentTypeService.GetByIdAsync(entry.ContentTypeId, ct);

            if (!ModelState.IsValid)
            {
                return View(new EntryFormViewModel
                {
                    IsNew = false,
                    Existing = entry,
                    ContentType = contentType,
                    Slug = model.Slug,
                    Locale = model.Locale,
                    Fields = model.Fields,
                    ChangeNote = model.ChangeNote,
                });
            }

            var fields = BuildFieldDictionary(model.Fields, contentType.Fields);
            await _entryService.UpdateAsync(id, new UpdateEntryRequest
            {
                Fields = fields,
                NewSlug = model.Slug != entry.Slug ? model.Slug : null,
                ChangeNote = model.ChangeNote,
            }, ct);

            AddSuccess("Entry saved.");
            return RedirectToAction(nameof(Edit), new { id });
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "Failed to update entry {Id}.", id);
            HandleApiError(ex);
            return View(model);
        }
    }

    // ── Workflow actions ─────────────────────────────────────────────────────

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Publish(string id, string? returnTo, CancellationToken ct)
    {
        try
        {
            await _entryService.PublishAsync(id, ct);
            AddSuccess("Entry published.");
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "Failed to publish entry {Id}.", id);
            AddError(ex.Message);
        }

        return returnTo == "edit"
            ? RedirectToAction(nameof(Edit), new { id })
            : RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Unpublish(string id, string? returnTo, CancellationToken ct)
    {
        try
        {
            await _entryService.UnpublishAsync(id, ct);
            AddSuccess("Entry unpublished.");
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "Failed to unpublish entry {Id}.", id);
            AddError(ex.Message);
        }

        return returnTo == "edit"
            ? RedirectToAction(nameof(Edit), new { id })
            : RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string id, string? contentTypeId, CancellationToken ct)
    {
        try
        {
            await _entryService.DeleteAsync(id, ct);
            AddSuccess("Entry deleted.");
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "Failed to delete entry {Id}.", id);
            AddError(ex.Message);
        }

        return RedirectToAction(nameof(Index), new { contentTypeId });
    }

    // ── Picker (JSON) ────────────────────────────────────────────────────────

    /// <summary>
    /// JSON endpoint used by the entry-reference picker modal in the entry editor.
    /// Returns a paged list of entries matching the search filter.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Picker(
        string? contentTypeId,
        string? search,
        int page = 1,
        CancellationToken ct = default)
    {
        try
        {
            var result = await _entryService.ListAsync(new EntryListParams
            {
                ContentTypeId = contentTypeId,
                Search = search,
                PageNumber = page,
                PageSize = 15,
            }, ct);

            return Json(result, new System.Text.Json.JsonSerializerOptions
            {
                PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
            });
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "Failed to load entries for picker.");
            return Json(new { items = Array.Empty<object>(), totalCount = 0 });
        }
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Converts string form values to typed objects using the field schema.
    /// Keeps cyclomatic complexity low by delegating type coercion to a helper.
    /// </summary>
    private static Dictionary<string, object?> BuildFieldDictionary(
        Dictionary<string, string?> formValues,
        IEnumerable<FieldDefinitionDto> fieldDefs)
    {
        var result = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

        foreach (var field in fieldDefs)
        {
            formValues.TryGetValue(field.Handle, out var raw);
            result[field.Handle] = CoerceFieldValue(field.FieldType, raw);
        }

        return result;
    }

    /// <summary>
    /// Coerces a raw string form value to the appropriate CLR type for a given field type.
    /// </summary>
    private static object? CoerceFieldValue(string fieldType, string? raw)
    {
        if (raw is null) return null;

        return fieldType switch
        {
            "Boolean" => raw == "true" || raw == "on",
            "Integer" => int.TryParse(raw, out var i) ? i : null,
            "Decimal" => decimal.TryParse(raw,
                System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture,
                out var d) ? d : null,
            _ => raw,
        };
    }
}
