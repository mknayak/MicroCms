using MicroCMS.Shared.Ids;

namespace MicroCMS.Ai.Abstractions.Interfaces;

/// <summary>
/// Provides site-scoped and tenant-scoped settings resolution for AI services.
/// Implemented in Infrastructure and injected into AI services via DI.
/// </summary>
public interface ISettingsReader
{
    /// <summary>
    /// Resolves the raw string value for <paramref name="key"/> using the site→tenant chain.
    /// Returns <c>null</c> when the key is absent.
    /// </summary>
    Task<string?> GetAsync(
        SiteId siteId,
        string key,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Resolves <paramref name="key"/> and converts to <typeparamref name="T"/>.
    /// Returns <paramref name="defaultValue"/> when the key is absent or conversion fails.
    /// </summary>
    Task<T> GetAsync<T>(
        SiteId siteId,
        string key,
        T defaultValue,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Resolves <paramref name="key"/> using tenant + optional site scope and converts to <typeparamref name="T"/>.
    /// When <paramref name="siteId"/> is null, only tenant-level entries are searched.
    /// Returns <paramref name="defaultValue"/> when the key is absent or conversion fails.
    /// </summary>
    Task<T> GetAsync<T>(
        TenantId tenantId,
        SiteId? siteId,
        string key,
        T defaultValue,
        CancellationToken cancellationToken = default);
}
