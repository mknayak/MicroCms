using MicroCMS.Domain.Aggregates.Tenant;
using MicroCMS.Domain.Specifications;

namespace MicroCMS.Domain.Specifications.Tenant;

/// <summary>Paged all-tenants specification for admin list.</summary>
public sealed class AllTenantsSpec : BaseSpecification<global::MicroCMS.Domain.Aggregates.Tenant.Tenant>
{
    public AllTenantsSpec(int page, int pageSize)
        : base(_ => true)
    {
        ApplyOrderBy(t => t.Slug);
        ApplyPaging((page - 1) * pageSize, pageSize);
    }
}

/// <summary>Count-only specification — no paging.</summary>
public sealed class AllTenantsCountSpec : BaseSpecification<global::MicroCMS.Domain.Aggregates.Tenant.Tenant>
{
    public AllTenantsCountSpec() : base(_ => true) { }
}
