using MicroCMS.Admin.Mvc.Models.ApiDtos;

namespace MicroCMS.Admin.Mvc.Models.ViewModels.Entries;

/// <summary>
/// View model shared by the Create and Edit entry forms.
/// </summary>
public sealed class EntryFormViewModel
{
    public bool IsNew { get; init; }

    /// <summary>Existing entry when editing; null when creating.</summary>
    public EntryDto? Existing { get; init; }

    /// <summary>The schema that drives the field editor.</summary>
    public ContentTypeDto ContentType { get; init; } = new();

    /// <summary>All available content types — used by the Create picker.</summary>
    public List<ContentTypeListItem> ContentTypes { get; init; } = [];

    // ── Editable metadata ────────────────────────────────────────────────────

    public string Slug { get; set; } = string.Empty;
    public string Locale { get; set; } = "en";
    public string? ChangeNote { get; set; }

    /// <summary>
    /// Field values keyed by handle, as plain strings from the HTML form.
    /// Converted to typed objects before the API call.
    /// </summary>
    public Dictionary<string, string?> Fields { get; set; } = [];

    // ── Helpers ──────────────────────────────────────────────────────────────

    /// <summary>Returns the current string value for a field handle.</summary>
    public string FieldValue(string handle)
    {
        // Prefer any value already submitted (POST round-trip)
        if (Fields.TryGetValue(handle, out var submitted) && submitted is not null)
            return submitted;

        // Fall back to the persisted entry value
        if (Existing?.Fields.TryGetValue(handle, out var persisted) == true)
            return persisted?.ToString() ?? string.Empty;

        return string.Empty;
    }
}
