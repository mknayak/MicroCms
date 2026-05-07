using MicroCMS.Domain.Aggregates.Content;
using MicroCMS.Domain.Enums;
using MicroCMS.Shared.Ids;

namespace MicroCMS.Domain.Specifications.Content;

/// <summary>
/// Returns entries for a site with optional status, content type, locale, and folder filters.
/// The paging overload is used by <c>ListEntriesQueryHandler</c> for data retrieval;
/// the non-paging overload is used for count-only queries.
/// </summary>
public sealed class EntriesBySiteSpec : BaseSpecification<Entry>
{
    /// <summary>Paginated constructor for data retrieval.</summary>
    public EntriesBySiteSpec(
        SiteId siteId,
        string? statusFilter,
        Guid? contentTypeId,
        string? locale,
        Guid? folderId,
        int pageNumber,
        int pageSize,
        string? search = null,
        string? sortBy = null,
        bool sortDesc = true)
        : base(BuildCriteria(siteId, statusFilter, contentTypeId, locale, folderId, search))
    {
        ApplySorting(sortBy, sortDesc);
        ApplyPaging((pageNumber - 1) * pageSize, pageSize);
    }

    /// <summary>Non-paged constructor for count-only queries.</summary>
    public EntriesBySiteSpec(
        SiteId siteId,
        string? statusFilter,
        Guid? contentTypeId = null,
        string? locale = null,
        Guid? folderId = null,
        string? search = null)
        : base(BuildCriteria(siteId, statusFilter, contentTypeId, locale, folderId, search))
    {
    }

    private void ApplySorting(string? sortBy, bool sortDesc)
    {
        var key = sortBy?.ToLowerInvariant() ?? string.Empty;

        if (key == "slug")       { OrderOn(e => (object)e.Slug, sortDesc); return; }
        if (key == "status")     { OrderOn(e => (int)e.Status, sortDesc); return; }
        if (key == "locale")     { OrderOn(e => (object)e.Locale, sortDesc); return; }
        if (key == "createdat")  { OrderOn(e => e.CreatedAt, sortDesc); return; }

        // default: updatedAt
        OrderOn(e => e.UpdatedAt, sortDesc);
    }

    private void OrderOn(System.Linq.Expressions.Expression<Func<Entry, object>> expr, bool desc)
    {
        if (desc) ApplyOrderByDescending(expr);
        else ApplyOrderBy(expr);
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("SonarLint", "S1541", Justification = "All conditions are required for filtering; extracting further would harm EF Core translateability.")]
    private static System.Linq.Expressions.Expression<System.Func<Entry, bool>> BuildCriteria(
        SiteId siteId,
        string? statusFilter,
        Guid? contentTypeId,
        string? locale,
        Guid? folderId,
        string? search)
    {
        // Parse the status string once outside the expression tree so EF Core
        // receives a plain enum value it can translate to an integer comparison.
        EntryStatus? status = Enum.TryParse<EntryStatus>(statusFilter, ignoreCase: true, out var parsed)
            ? parsed
            : null;

        // Normalise search term outside the expression tree.
        var term = string.IsNullOrWhiteSpace(search) ? null : search.Trim().ToLowerInvariant();

        return e => e.SiteId == siteId
                 && (status == null || e.Status == status.Value)
                 && (contentTypeId == null || e.ContentTypeId == new ContentTypeId(contentTypeId.Value))
                 && (locale == null || e.Locale.Value == locale)
                 && (folderId == null || (e.FolderId != null && e.FolderId.Value.Value == folderId.Value))
                 && (term == null || e.Slug.Value.ToLower().Contains(term) || e.FieldsJson.ToLower().Contains(term));
    }
}
