using MicroCMS.Domain.Aggregates.Content;
using MicroCMS.Domain.ValueObjects;
using MicroCMS.Shared.Ids;

namespace MicroCMS.Domain.Specifications.Content;

/// <summary>
/// Returns the entry matching the given slug within a specific site and locale.
/// Used by slug-uniqueness validation in CreateEntry and UpdateEntry commands.
/// </summary>
public sealed class EntryBySlugAndSiteSpec : BaseSpecification<Entry>
{
    public EntryBySlugAndSiteSpec(SiteId siteId, string slug, string locale)
        : this(siteId, Slug.Create(slug), Locale.Create(locale)) { }

    private EntryBySlugAndSiteSpec(SiteId siteId, Slug slug, Locale locale)
        : base(e => e.SiteId == siteId
                 && e.Slug == slug
    && e.Locale == locale) { }
}
