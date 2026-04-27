using MicroCMS.Domain.Aggregates.Content;
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
        : base(e => e.SiteId == siteId
                 && (statusFilter == null || e.Status.ToString() == statusFilter)
                 && (contentTypeId == null || e.ContentTypeId == new ContentTypeId(contentTypeId.Value))
                 && (locale == null || e.Locale.Value == locale)
                 && (folderId == null || (e.FolderId != null && e.FolderId.Value.Value == folderId.Value)))
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
        : base(e => e.SiteId == siteId
                 && (statusFilter == null || e.Status.ToString() == statusFilter)
                 && (contentTypeId == null || e.ContentTypeId == new ContentTypeId(contentTypeId.Value))
                 && (locale == null || e.Locale.Value == locale)
                 && (folderId == null || (e.FolderId != null && e.FolderId.Value.Value == folderId.Value)))
    {
    }
}
