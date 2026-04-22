using MicroCMS.Domain.Aggregates.Tenant;
using MicroCMS.Domain.Enums;

namespace MicroCMS.Domain.Specifications.Tenants;

/// <summary>Returns all tenants currently in Active status.</summary>
public sealed class ActiveTenantsSpec : BaseSpecification<Tenant>
{
    public ActiveTenantsSpec()
        : base(t => t.Status == TenantStatus.Active)
    {
        ApplyOrderBy(t => t.Slug.Value);
    }
}
