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
        int pageSize)
        : base(BuildCriteria(siteId, statusFilter, contentTypeId, locale, folderId))
    {
        ApplyOrderByDescending(e => e.UpdatedAt);
        ApplyPaging((pageNumber - 1) * pageSize, pageSize);
    }

    /// <summary>Non-paged constructor for count-only queries.</summary>
    public EntriesBySiteSpec(
        SiteId siteId,
        string? statusFilter,
        Guid? contentTypeId = null,
        string? locale = null,
        Guid? folderId = null)
        : base(BuildCriteria(siteId, statusFilter, contentTypeId, locale, folderId))
    {
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("SonarLint", "S1541", Justification = "All conditions are required for filtering; extracting further would harm EF Core translateability.")]
    private static System.Linq.Expressions.Expression<System.Func<Entry, bool>> BuildCriteria(
        SiteId siteId,
        string? statusFilter,
        Guid? contentTypeId,
        string? locale,
        Guid? folderId)
    {
        // Parse the status string once outside the expression tree so EF Core
        // receives a plain enum value it can translate to an integer comparison.
        EntryStatus? status = Enum.TryParse<EntryStatus>(statusFilter, ignoreCase: true, out var parsed)
            ? parsed
            : null;

        return e => e.SiteId == siteId
                 && (status == null || e.Status == status.Value)
                 && (contentTypeId == null || e.ContentTypeId == new ContentTypeId(contentTypeId.Value))
                 && (locale == null || e.Locale.Value == locale)
                 && (folderId == null || (e.FolderId != null && e.FolderId.Value.Value == folderId.Value));
    }
}
