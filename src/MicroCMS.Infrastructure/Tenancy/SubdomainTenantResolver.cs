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
/// Resolves <see cref="TenantId"/> from the incoming request using the following priority:
///   1. Subdomain of the <c>Host</c> header (e.g. <c>acme.microcms.io</c> → <c>acme</c>).
///   2. <c>X-Tenant-Slug</c> header — only honoured for <c>SystemAdmin</c> JWT callers.
///   3. JWT <c>tid</c> / <c>tenant_id</c> / <c>tenantSlug</c> claim (covers localhost / direct-IP).
/// Results are cached in-process for <see cref="CacheTtl"/> to avoid DB hits on every request.
/// </summary>
internal sealed class SubdomainTenantResolver(
    IHttpContextAccessor httpContextAccessor,
    IRepository<Tenant, TenantId> tenantRepo,
    ILogger<SubdomainTenantResolver> logger)
    : ITenantResolver
{
    internal static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);
    private const int MaxCacheSize = 1024;

    private static readonly ConcurrentDictionary<string, (TenantId? Id, DateTimeOffset CachedAt)>
        _cache = new(StringComparer.OrdinalIgnoreCase);

    public async Task<TenantId?> ResolveAsync(CancellationToken cancellationToken = default)
    {
        var ctx = httpContextAccessor.HttpContext;
        if (ctx is null) return null;

        var slug = ExtractSlug(ctx);
        if (string.IsNullOrEmpty(slug)) return null;

        if (_cache.TryGetValue(slug, out var entry) &&
            DateTimeOffset.UtcNow - entry.CachedAt < CacheTtl)
            return entry.Id;

        var tenants = await tenantRepo.ListAsync(new TenantBySlugSpec(slug), cancellationToken);
        var tenantId = tenants.Count > 0 ? tenants[0].Id : (TenantId?)null;

        EvictIfAtCapacity();
        _cache[slug] = (tenantId, DateTimeOffset.UtcNow);

        if (tenantId is null)
            logger.LogWarning("Tenant slug '{Slug}' could not be resolved to a known tenant.", slug);

        return tenantId;
    }

    // ── Slug extraction (three independent fallbacks) ─────────────────────

    private static string? ExtractSlug(HttpContext ctx) =>
      ExtractFromSubdomain(ctx.Request.Host.Host)
        ?? ExtractFromTenantSlugHeader(ctx)
      ?? ExtractFromJwtClaims(ctx);

    private static string? ExtractFromSubdomain(string host)
    {
        var dot = host.IndexOf('.', StringComparison.Ordinal);
     if (dot <= 0) return null;

    var sub = host[..dot];
        return sub.Equals("www", StringComparison.OrdinalIgnoreCase) ? null : sub;
    }

    private static string? ExtractFromTenantSlugHeader(HttpContext ctx)
    {
        var slug = ctx.Request.Headers["X-Tenant-Slug"].FirstOrDefault();
        if (string.IsNullOrEmpty(slug)) return null;

        return ctx.User?.IsInRole("SystemAdmin") is true ? slug : null;
    }

    private static string? ExtractFromJwtClaims(HttpContext ctx) =>
        ctx.User?.FindFirst("tid")?.Value
   ?? ctx.User?.FindFirst("tenant_id")?.Value
     ?? ctx.User?.FindFirst("tenantSlug")?.Value;

    // ── Cache helpers ─────────────────────────────────────────────────────

    private static void EvictIfAtCapacity()
    {
    if (_cache.Count < MaxCacheSize) return;

        var oldest = _cache.MinBy(kv => kv.Value.CachedAt);
        _cache.TryRemove(oldest.Key, out _);
    }
}
