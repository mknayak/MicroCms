using MicroCMS.Domain.Exceptions;
using MicroCMS.Shared.Ids;

namespace MicroCMS.Domain.Aggregates.Settings;

/// <summary>
/// Tenant-level key-value configuration store (GAP-AI-1).
///
/// One <see cref="TenantConfig"/> exists per <c>Tenant</c> (1-to-1).
/// The aggregate ID is the <see cref="TenantId"/> so repository look-ups are always
/// <c>GetByIdAsync(tenantId)</c> — no secondary index needed.
///
/// Site-level overrides live in
/// <see cref="MicroCMS.Domain.Aggregates.Tenant.SiteSettings.ConfigEntries"/>.
/// Resolution chain: site → tenant → caller default.
/// </summary>
public sealed class TenantConfig : AggregateRoot<TenantId>
{
    private readonly List<ConfigEntry> _entries = [];

    private TenantConfig() : base() { } // EF Core

    private TenantConfig(TenantId tenantId) : base(tenantId)
    {
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public DateTimeOffset UpdatedAt { get; private set; }

    public IReadOnlyList<ConfigEntry> Entries => _entries.AsReadOnly();

    // ── Factory ────────────────────────────────────────────────────────────

    public static TenantConfig Create(TenantId tenantId)
    {
        ArgumentNullException.ThrowIfNull(tenantId);
        return new TenantConfig(tenantId);
    }

    // ── Mutations ─────────────────────────────────────────────────────────

    /// <summary>Inserts or replaces the entry identified by <paramref name="key"/>.</summary>
    public void UpsertEntry(string key, string value, string category = "general", bool isSecret = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key, nameof(key));

        var existing = _entries.Find(e => e.Key == key);
        if (existing is not null)
        {
            existing.Update(value, category, isSecret);
        }
        else
        {
            _entries.Add(ConfigEntry.Create(key, value, category, isSecret));
        }

        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>Removes the entry with the given <paramref name="key"/>. No-op if absent.</summary>
    public void RemoveEntry(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key, nameof(key));
        _entries.RemoveAll(e => e.Key == key);
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>Returns the entry for <paramref name="key"/>, or <c>null</c> if not found.</summary>
    public ConfigEntry? GetEntry(string key) =>
        _entries.Find(e => e.Key == key);
}
