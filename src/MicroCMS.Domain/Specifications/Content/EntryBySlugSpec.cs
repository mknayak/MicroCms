using MicroCMS.Domain.Aggregates.Content;
using MicroCMS.Domain.Enums;
using MicroCMS.Shared.Ids;

namespace MicroCMS.Domain.Specifications.Content;

/// <summary>Finds a published entry by its slug within a tenant+site+locale context.</summary>
public sealed class EntryBySlugSpec : BaseSpecification<Entry>
{
    public EntryBySlugSpec(TenantId tenantId, SiteId siteId, string slug, string locale)
        : base(e => e.TenantId == tenantId
                 && e.SiteId == siteId
                 && e.Slug.Value == slug
                 && e.Locale.Value == locale
                 && e.Status == EntryStatus.Published) { }
}
