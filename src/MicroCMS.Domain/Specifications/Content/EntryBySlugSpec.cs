using MicroCMS.Domain.Aggregates.Content;
using MicroCMS.Domain.Enums;
using MicroCMS.Domain.ValueObjects;
using MicroCMS.Shared.Ids;

namespace MicroCMS.Domain.Specifications.Content;

/// <summary>Finds a published entry by its slug within a tenant+site+locale context.</summary>
public sealed class EntryBySlugSpec : BaseSpecification<Entry>
{
    public EntryBySlugSpec(TenantId tenantId, SiteId siteId, string slug, string locale)
        : this(tenantId, siteId, Slug.Create(slug), Locale.Create(locale)) { }

    private EntryBySlugSpec(TenantId tenantId, SiteId siteId, Slug slug, Locale locale)
        : base(e => e.TenantId == tenantId
        && e.SiteId == siteId
      && e.Slug == slug
        && e.Locale == locale
       && e.Status == EntryStatus.Published) { }
}
