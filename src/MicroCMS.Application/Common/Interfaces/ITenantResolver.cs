using MicroCMS.Shared.Ids;

namespace MicroCMS.Application.Common.Interfaces;

/// <summary>
/// Resolves the current request's TenantId from the HTTP context
/// (e.g. subdomain, custom domain, or JWT claim fallback).
/// Implementations are expected to cache lookups to avoid per-request DB hits.
/// </summary>
public interface ITenantResolver
{
    /// <summary>
    /// Returns the <see cref="TenantId"/> for the current request, or
    /// <c>null</c> if no tenant could be resolved (e.g. system admin paths).
  /// </summary>
    Task<TenantId?> ResolveAsync(CancellationToken cancellationToken = default);
}
