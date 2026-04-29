using MicroCMS.Shared.Ids;

namespace MicroCMS.Application.Common.Interfaces;

/// <summary>
/// Reads configuration entries with site-then-tenant resolution (GAP-AI-1).
///
/// Resolution chain:
///   1. Site-level override  (keyed by <paramref name="siteId"/>)
///   2. Tenant-level default
///   3. Returns <c>null</c> / caller-supplied default
///
/// Implementations use cache-aside (TTL 5 min) to avoid a DB round-trip on every AI call.
/// Cache is invalidated via tag <c>settings:{tenantId}</c> when entries are written.
/// </summary>
public interface ISettingsReader
{
    /// <summary>
    /// Resolves the raw string value for <paramref name="key"/> using the
    /// site → tenant resolution chain.
    /// Returns <c>null</c> when the key is absent at both scopes.
    /// </summary>
    Task<string?> GetAsync(
        SiteId siteId,
        string key,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Resolves <paramref name="key"/> and converts the raw string to
    /// <typeparamref name="T"/> via <see cref="Convert.ChangeType"/>.
    /// Returns <paramref name="defaultValue"/> when the key is absent or conversion fails.
    /// Supported types: <see cref="string"/>, <see cref="bool"/>, <see cref="int"/>,
    /// <see cref="long"/>, <see cref="float"/>, <see cref="double"/>.
    /// </summary>
    Task<T> GetAsync<T>(
        SiteId siteId,
        string key,
        T defaultValue,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Invalidates all cached settings for the tenant that owns <paramref name="siteId"/>.
    /// Call this after any write to <c>SiteSettings.ConfigEntries</c> or <c>TenantConfig.Entries</c>.
    /// </summary>
    Task InvalidateAsync(
        SiteId siteId,
        CancellationToken cancellationToken = default);
}
