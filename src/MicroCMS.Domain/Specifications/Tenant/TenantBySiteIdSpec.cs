using MicroCMS.Domain.Specifications;
using MicroCMS.Shared.Ids;

namespace MicroCMS.Domain.Specifications.Tenant;

/// <summary>
/// Finds the <see cref="MicroCMS.Domain.Aggregates.Tenant.Tenant"/> that owns
/// the site with the given <paramref name="siteId"/>.
/// Used when only the SiteId is known (e.g. site-settings endpoints).
/// </summary>
public sealed class TenantBySiteIdSpec
    : BaseSpecification<global::MicroCMS.Domain.Aggregates.Tenant.Tenant>
{
    public TenantBySiteIdSpec(SiteId siteId)
        : base(t => t.Sites.Any(s => s.Id == siteId)) { }
}
