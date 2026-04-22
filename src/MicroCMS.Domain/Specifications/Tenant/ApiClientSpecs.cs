using MicroCMS.Domain.Aggregates.Tenant;

namespace MicroCMS.Domain.Specifications.Tenant;

/// <summary>Looks up an API client by the SHA-256 hash of its secret key.</summary>
public sealed class ApiClientByHashSpec : BaseSpecification<ApiClient>
{
    public ApiClientByHashSpec(string hashedSecret)
        : base(c => c.HashedSecret == hashedSecret)
    {
    }
}

/// <summary>Returns all active API clients for a given site.</summary>
public sealed class ApiClientsBySiteSpec : BaseSpecification<ApiClient>
{
    public ApiClientsBySiteSpec(MicroCMS.Shared.Ids.SiteId siteId)
        : base(c => c.SiteId == siteId && c.IsActive)
    {
        ApplyOrderByDescending(c => c.CreatedAt);
    }
}
