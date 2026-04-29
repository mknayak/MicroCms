using MicroCMS.Application.Common.Interfaces;
using MicroCMS.Domain.Aggregates.Settings;
using MicroCMS.Domain.Aggregates.Tenant;
using MicroCMS.Domain.Repositories;
using MicroCMS.Shared.Ids;
using Microsoft.Extensions.Logging;

namespace MicroCMS.Infrastructure.Settings;

/// <summary>
/// Resolves configuration entries via a site → tenant chain (GAP-AI-1).
///
/// Cache strategy:
///   Key  <c>settings:site:{siteId}</c>    → site-level entries + owning TenantId
///   Key  <c>settings:tenant:{tenantId}</c> → tenant-level entries
///   Tag  <c>settings:{tenantId}</c>         → both keys; invalidated together on write
///   TTL  5 minutes
/// </summary>
internal sealed class SettingsReader : ISettingsReader
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);

    private readonly IRepository<SiteSettings, SiteId> _siteSettingsRepo;
    private readonly IRepository<TenantConfig, TenantId> _tenantConfigRepo;
    private readonly ICacheService _cache;
    private readonly ILogger<SettingsReader> _logger;

    public SettingsReader(
        IRepository<SiteSettings, SiteId> siteSettingsRepo,
        IRepository<TenantConfig, TenantId> tenantConfigRepo,
        ICacheService cache,
        ILogger<SettingsReader> logger)
    {
        _siteSettingsRepo = siteSettingsRepo;
        _tenantConfigRepo = tenantConfigRepo;
        _cache = cache;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<string?> GetAsync(
        SiteId siteId,
        string key,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key, nameof(key));

        var (siteEntries, tenantId) = await LoadSiteEntriesAsync(siteId, cancellationToken);

        // 1. Site-level override
        var siteHit = siteEntries.FirstOrDefault(e => e.Key == key);
        if (siteHit is not null)
            return siteHit.Value;

        if (tenantId is null)
            return null;

        // 2. Tenant-level default
        var tenantEntries = await LoadTenantEntriesAsync(tenantId.Value, cancellationToken);
        return tenantEntries.FirstOrDefault(e => e.Key == key)?.Value;
    }

    /// <inheritdoc/>
    public async Task<T> GetAsync<T>(
        SiteId siteId,
        string key,
        T defaultValue,
        CancellationToken cancellationToken = default)
    {
        var raw = await GetAsync(siteId, key, cancellationToken);

        if (raw is null)
            return defaultValue;

        try
        {
            return (T)Convert.ChangeType(raw, typeof(T));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Settings key '{Key}' value '{Value}' cannot be converted to {Type}. Using default.",
                key, raw, typeof(T).Name);

            return defaultValue;
        }
    }

    /// <inheritdoc/>
    public async Task InvalidateAsync(SiteId siteId, CancellationToken cancellationToken = default)
    {
        // Bypass cache to get the tenantId needed for tag invalidation.
        var settings = await _siteSettingsRepo.GetByIdAsync(siteId, cancellationToken);
        if (settings is null)
            return;

        await _cache.RemoveByTagAsync(SettingsCacheTag(settings.TenantId), cancellationToken);

        _logger.LogDebug(
            "Invalidated settings cache for tenant {TenantId} (triggered by site {SiteId}).",
            settings.TenantId, siteId);
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private async Task<(IReadOnlyList<SettingEntry> Entries, TenantId? TenantId)> LoadSiteEntriesAsync(
        SiteId siteId,
        CancellationToken cancellationToken)
    {
        var cacheKey = SiteCacheKey(siteId);
        var cached = await _cache.GetAsync<SiteSettingsCachePayload>(cacheKey, cancellationToken);

        if (cached is not null)
            return (cached.Entries, cached.TenantId);

        var settings = await _siteSettingsRepo.GetByIdAsync(siteId, cancellationToken);
        if (settings is null)
            return (Array.Empty<SettingEntry>(), null);

        var payload = new SiteSettingsCachePayload(
            settings.TenantId,
            settings.ConfigEntries
                .Select(e => new SettingEntry(e.Key, e.Value, e.IsSecret))
                .ToList());

        await _cache.SetWithTagAsync(
            cacheKey, payload, SettingsCacheTag(settings.TenantId), CacheTtl, cancellationToken);

        return (payload.Entries, payload.TenantId);
    }

    private async Task<IReadOnlyList<SettingEntry>> LoadTenantEntriesAsync(
        TenantId tenantId,
        CancellationToken cancellationToken)
    {
        var cacheKey = TenantCacheKey(tenantId);
        var cached = await _cache.GetAsync<List<SettingEntry>>(cacheKey, cancellationToken);

        if (cached is not null)
            return cached;

        var config = await _tenantConfigRepo.GetByIdAsync(tenantId, cancellationToken);
        if (config is null)
            return Array.Empty<SettingEntry>();

        var entries = config.Entries
            .Select(e => new SettingEntry(e.Key, e.Value, e.IsSecret))
            .ToList();

        await _cache.SetWithTagAsync(
            cacheKey, entries, SettingsCacheTag(tenantId), CacheTtl, cancellationToken);

        return entries;
    }

    // ── Cache key helpers ─────────────────────────────────────────────────

    private static string SiteCacheKey(SiteId siteId) =>
        $"settings:site:{siteId.Value}";

    private static string TenantCacheKey(TenantId tenantId) =>
        $"settings:tenant:{tenantId.Value}";

    private static string SettingsCacheTag(TenantId tenantId) =>
        $"settings:{tenantId.Value}";

    // ── Cache payload types ───────────────────────────────────────────────

    private sealed record SiteSettingsCachePayload(
        TenantId TenantId,
        IReadOnlyList<SettingEntry> Entries);

    /// <summary>Lightweight projection — avoids caching domain aggregate instances.</summary>
    private sealed record SettingEntry(string Key, string Value, bool IsSecret);
}
