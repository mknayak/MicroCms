using System.Collections.Concurrent;
using MicroCMS.Application.Common.Interfaces;
using MicroCMS.Domain.Aggregates.Tenant;
using MicroCMS.Domain.Repositories;
using MicroCMS.Domain.Specifications.Tenant;
using MicroCMS.Shared.Ids;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace MicroCMS.Infrastructure.Tenancy;

/// <summary>
/// Resolves <see cref="TenantId"/> from the incoming request's subdomain
/// (e.g. <c>acme.microcms.io</c> → slug <c>acme</c>).
///
/// Resolution order:
///   1. Subdomain of <c>Host</c> header.
///   2. <c>X-Tenant-Slug</c> header — only honoured when the caller has the
///      <c>SystemAdmin</c> JWT role; otherwise silently ignored (SSRF / spoofing guard).
///
/// Results are cached in an in-process LRU cache (<see cref="_cache"/>) to avoid
/// a DB round-trip on every request. Cache entries expire after
/// <see cref="CacheTtl"/> to pick up slug-to-tenant changes within a reasonable window.
/// </summary>
internal sealed class SubdomainTenantResolver(
    IHttpContextAccessor httpContextAccessor,
    IRepository<Tenant, TenantId> tenantRepo,
    ILogger<SubdomainTenantResolver> logger)
    : ITenantResolver
{
    internal static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);
 private const int MaxCacheSize = 1024;

    // slug → (TenantId?, cachedAt)
    private static readonly ConcurrentDictionary<string, (TenantId? Id, DateTimeOffset CachedAt)> _cache = new(StringComparer.OrdinalIgnoreCase);

    public async Task<TenantId?> ResolveAsync(CancellationToken cancellationToken = default)
    {
        var ctx = httpContextAccessor.HttpContext;
        if (ctx is null) return null;

        var slug = ExtractSlug(ctx);
        if (string.IsNullOrEmpty(slug)) return null;

        // Cache hit
        if (_cache.TryGetValue(slug, out var entry) &&
            DateTimeOffset.UtcNow - entry.CachedAt < CacheTtl)
        {
   return entry.Id;
        }

        // Cache miss — query DB
        var tenants = await tenantRepo.ListAsync(new TenantBySlugSpec(slug), cancellationToken);
   var tenantId = tenants.Count > 0 ? tenants[0].Id : (TenantId?)null;

        // Evict oldest entry if at capacity (simple LRU approximation)
      if (_cache.Count >= MaxCacheSize)
        {
          var oldest = _cache.OrderBy(kv => kv.Value.CachedAt).FirstOrDefault();
  _cache.TryRemove(oldest.Key, out _);
        }

        _cache[slug] = (tenantId, DateTimeOffset.UtcNow);

  if (tenantId is null)
        logger.LogWarning("Tenant slug '{Slug}' could not be resolved to a known tenant.", slug);

        return tenantId;
    }

  private static string? ExtractSlug(HttpContext ctx)
 {
        var host = ctx.Request.Host.Host; // e.g. "acme.microcms.io" or "localhost"

        // Subdomain extraction: first label before first dot
     var firstDot = host.IndexOf('.', StringComparison.Ordinal);
      if (firstDot > 0)
        {
      var subdomain = host[..firstDot];
   // Ignore "www" and bare hostnames like "localhost"
   if (!subdomain.Equals("www", StringComparison.OrdinalIgnoreCase))
     return subdomain;
        }

        // Fallback: X-Tenant-Slug header — only trusted from SystemAdmin callers
  var headerSlug = ctx.Request.Headers["X-Tenant-Slug"].FirstOrDefault();
        if (!string.IsNullOrEmpty(headerSlug))
     {
         var isSystemAdmin = ctx.User?.IsInRole("SystemAdmin") is true;
          if (isSystemAdmin)
      return headerSlug;
}

        return null;
    }
}
