using MicroCMS.Domain.Aggregates.Content;
using MicroCMS.Domain.Enums;
using MicroCMS.Domain.Specifications;
using MicroCMS.Domain.ValueObjects;
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
        : this(siteId, contentTypeId, locale == null ? null : Locale.Create(locale), page, pageSize) { }

    private PublishedEntriesByContentTypeSpec(
        SiteId siteId,
        ContentTypeId contentTypeId,
        Locale? locale,
        int page,
        int pageSize)
        : base(e => e.SiteId == siteId
          && e.ContentTypeId == contentTypeId
                 && e.Status == EntryStatus.Published
      && (locale == null || e.Locale == locale))
    {
        ApplyOrderByDescending(e => e.PublishedAt!);
        ApplyPaging((page - 1) * pageSize, pageSize);
    }

    /// <summary>Count-only overload (no paging).</summary>
    public PublishedEntriesByContentTypeSpec(SiteId siteId, ContentTypeId contentTypeId, string? locale)
        : this(siteId, contentTypeId, locale == null ? null : Locale.Create(locale)) { }

    private PublishedEntriesByContentTypeSpec(SiteId siteId, ContentTypeId contentTypeId, Locale? locale)
: base(e => e.SiteId == siteId
       && e.ContentTypeId == contentTypeId
                 && e.Status == EntryStatus.Published
         && (locale == null || e.Locale == locale))
    { }
}

/// <summary>A single published entry by slug + site + locale.</summary>
public sealed class PublishedEntryBySlugSpec : BaseSpecification<Entry>
{
    public PublishedEntryBySlugSpec(SiteId siteId, string slug, string locale)
        : this(siteId, Slug.Create(slug), Locale.Create(locale)) { }

    private PublishedEntryBySlugSpec(SiteId siteId, Slug slug, Locale locale)
 : base(e => e.SiteId == siteId
             && e.Slug == slug
             && e.Locale == locale
 && e.Status == EntryStatus.Published)
    { }
}
