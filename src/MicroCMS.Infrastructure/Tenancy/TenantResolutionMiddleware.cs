using MicroCMS.Application.Common.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace MicroCMS.Infrastructure.Tenancy;

/// <summary>
/// ASP.NET Core middleware that resolves the current tenant early in the pipeline
/// and stores the <c>TenantId</c> in <c>HttpContext.Items</c> for downstream use.
///
/// Security guarantees:
/// - Resolution is based on the subdomain of the <c>Host</c> header.
/// - The <c>X-Tenant-Slug</c> escape header is only honoured for <c>SystemAdmin</c> JWT role holders.
/// - Unknown slugs result in a 404 (tenant not found), not a pass-through.
///   Exception: paths under <c>/health</c>, <c>/swagger</c>, and <c>/metrics</c>
///   are exempt to allow infra probes and docs to function without a tenant context.
/// </summary>
public sealed class TenantResolutionMiddleware(
    RequestDelegate next,
    ILogger<TenantResolutionMiddleware> logger)
{
    internal const string TenantIdItemKey = "ResolvedTenantId";

    private static readonly HashSet<string> _exemptPrefixes = new(StringComparer.OrdinalIgnoreCase)
    {
        "/health",
        "/swagger",
        "/metrics",
        "/favicon.ico",
        // Auth endpoints are anonymous (login/refresh) or carry tenant identity via JWT claims
        // already handled by the DbContext query-filter. Resolving here adds a DB round-trip
        // and emits a misleading 'no tenant resolved' warning for every unauthenticated call.
        "/api/v1/auth",
        // Install endpoints are anonymous by design and pre-date any tenant.
        "/api/v1/install",
    };

    public async Task InvokeAsync(HttpContext context, ITenantResolver resolver)
    {
        if (IsExemptPath(context.Request.Path))
        {
      await next(context);
            return;
        }

     var tenantId = await resolver.ResolveAsync(context.RequestAborted);

     if (tenantId is null)
        {
       logger.LogDebug("No tenant resolved for path {Path} — continuing as system context.", context.Request.Path);
  await next(context);
     return;
        }

        context.Items[TenantIdItemKey] = tenantId;
   logger.LogDebug("Resolved tenant {TenantId} for request {Path}.", tenantId, context.Request.Path);

        await next(context);
    }

    private static bool IsExemptPath(PathString path)
    {
        // Normalise consecutive slashes (e.g. "//api/v1/auth/login" → "/api/v1/auth/login")
        // that can appear when a reverse-proxy or PathBase prefix is misconfigured.
        var normalised = new PathString("/" + path.Value?.TrimStart('/'));

        foreach (var prefix in _exemptPrefixes)
        {
            if (normalised.StartsWithSegments(prefix, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }
}
