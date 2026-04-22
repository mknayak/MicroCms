using MicroCMS.Shared.Ids;

namespace MicroCMS.Application.Common.Interfaces;

/// <summary>
/// Enforces per-tenant resource quotas.
/// Called by application-layer handlers before operations that consume
/// metered resources (storage, API calls, user seats, sites, content types).
/// </summary>
public interface IQuotaService
{
 /// <summary>Throws <see cref="QuotaExceededException"/> if the tenant has no remaining storage.</summary>
    Task EnforceStorageAsync(TenantId tenantId, long additionalBytes, CancellationToken ct = default);

    /// <summary>Throws <see cref="QuotaExceededException"/> if the tenant has reached its user cap.</summary>
    Task EnforceUserCountAsync(TenantId tenantId, CancellationToken ct = default);

    /// <summary>Throws <see cref="QuotaExceededException"/> if the tenant has reached its site cap.</summary>
    Task EnforceSiteCountAsync(TenantId tenantId, CancellationToken ct = default);

    /// <summary>Throws <see cref="QuotaExceededException"/> if the tenant has reached its content-type cap.</summary>
    Task EnforceContentTypeCountAsync(TenantId tenantId, CancellationToken ct = default);
}
