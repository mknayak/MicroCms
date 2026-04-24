using MicroCMS.Domain.Aggregates.Tenant;
using MicroCMS.Domain.Specifications;
using MicroCMS.Domain.ValueObjects;

namespace MicroCMS.Domain.Specifications.Tenant;

/// <summary>Finds a tenant by its unique slug.</summary>
public sealed class TenantBySlugSpec : BaseSpecification<global::MicroCMS.Domain.Aggregates.Tenant.Tenant>
{
    public TenantBySlugSpec(string slug)
        : base(t => t.Slug == TenantSlug.Create(slug)) { }
}
