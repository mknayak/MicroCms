using MicroCMS.Domain.Aggregates.Content;
using MicroCMS.Domain.Enums;
using MicroCMS.Domain.Specifications;
using MicroCMS.Shared.Ids;

namespace MicroCMS.Domain.Specifications.Delivery;

/// <summary>Published entries for a site filtered by ContentTypeId. Paged.</summary>
public sealed class PublishedEntriesByContentTypeSpec : BaseSpecification<Entry>
{
    public PublishedEntriesByContentTypeSpec(
        SiteId siteId,
    ContentTypeId contentTypeId,
        string? locale,
        int page,
        int pageSize)
        : base(e => e.SiteId == siteId
       && e.ContentTypeId == contentTypeId
          && e.Status == EntryStatus.Published
  && (locale == null || e.Locale.Value == locale))
    {
      ApplyOrderByDescending(e => e.PublishedAt!);
        ApplyPaging((page - 1) * pageSize, pageSize);
    }

    /// <summary>Count-only overload (no paging).</summary>
    public PublishedEntriesByContentTypeSpec(SiteId siteId, ContentTypeId contentTypeId, string? locale)
: base(e => e.SiteId == siteId
          && e.ContentTypeId == contentTypeId
 && e.Status == EntryStatus.Published
     && (locale == null || e.Locale.Value == locale))
    {
    }
}

/// <summary>A single published entry by slug + site + locale.</summary>
public sealed class PublishedEntryBySlugSpec : BaseSpecification<Entry>
{
    public PublishedEntryBySlugSpec(SiteId siteId, string slug, string locale)
        : base(e => e.SiteId == siteId
      && e.Slug.Value == slug
       && e.Locale.Value == locale
                 && e.Status == EntryStatus.Published)
    {
    }
}
