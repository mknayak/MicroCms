using MicroCMS.Application.Common.Exceptions;
using MicroCMS.Application.Common.Interfaces;
using MicroCMS.Domain.Aggregates.Content;
using MicroCMS.Domain.Aggregates.Identity;
using MicroCMS.Domain.Aggregates.Tenant;
using MicroCMS.Domain.Repositories;
using MicroCMS.Domain.Specifications.Content;
using MicroCMS.Domain.Specifications.Identity;
using MicroCMS.Domain.Specifications.Tenant;
using MicroCMS.Shared.Ids;

namespace MicroCMS.Infrastructure.Tenancy;

/// <summary>
/// Checks per-tenant resource quotas against the tenant's configured <see cref="TenantQuota"/>.
/// Counts are read from the repository (query filter scopes to the tenant automatically).
///
/// Zero quota values mean "unlimited" (see <see cref="MicroCMS.Domain.ValueObjects.TenantQuota"/>).
/// </summary>
internal sealed class QuotaService(
    IRepository<Tenant, TenantId> tenantRepo,
    IRepository<ContentType, ContentTypeId> contentTypeRepo,
    IRepository<User, UserId> userRepo)
    : IQuotaService
{
    public async Task EnforceStorageAsync(TenantId tenantId, long additionalBytes, CancellationToken ct = default)
    {
      var tenant = await GetTenantAsync(tenantId, ct);
      var max = tenant.Quota.MaxStorageBytes;
        if (max == 0) return; // unlimited

     // Storage byte tracking will be implemented in Sprint 7 with the full media pipeline.
        // For now the check passes; the actual used-bytes counter lives on MediaAsset aggregates.
 await Task.CompletedTask;
    }

    public async Task EnforceUserCountAsync(TenantId tenantId, CancellationToken ct = default)
    {
        var tenant = await GetTenantAsync(tenantId, ct);
        var max = tenant.Quota.MaxUsers;
        if (max == 0) return;

        var current = await userRepo.CountAsync(new AllUsersCountSpec(), ct);
        if (current >= max)
      throw new QuotaExceededException("MaxUsers", $"Tenant has reached its user limit of {max}.");
 }

    public async Task EnforceSiteCountAsync(TenantId tenantId, CancellationToken ct = default)
    {
        var tenant = await GetTenantAsync(tenantId, ct);
     var max = tenant.Quota.MaxSites;
     if (max == 0) return;

        var current = tenant.Sites.Count;
        if (current >= max)
   throw new QuotaExceededException("MaxSites", $"Tenant has reached its site limit of {max}.");
    }

    public async Task EnforceContentTypeCountAsync(TenantId tenantId, CancellationToken ct = default)
    {
        var tenant = await GetTenantAsync(tenantId, ct);
        var max = tenant.Quota.MaxContentTypes;
        if (max == 0) return;

 var current = await contentTypeRepo.CountAsync(new ContentTypesBySiteSpec(SiteId.New()), ct);
     // Note: a full cross-site count requires a dedicated spec; placeholder uses the pattern.
     // Full implementation in Sprint 5 follow-up once site-aggregated count spec is added.
  if (current >= max)
        throw new QuotaExceededException("MaxContentTypes", $"Tenant has reached its content type limit of {max}.");
    }

    private async Task<Tenant> GetTenantAsync(TenantId tenantId, CancellationToken ct)
        => await tenantRepo.GetByIdAsync(tenantId, ct)
    ?? throw new NotFoundException(nameof(Tenant), tenantId);
}
