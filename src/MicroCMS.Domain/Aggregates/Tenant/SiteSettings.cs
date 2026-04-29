using MicroCMS.Domain.Aggregates.Settings;
using MicroCMS.Domain.Exceptions;
using MicroCMS.Domain.ValueObjects;
using MicroCMS.Shared.Ids;

namespace MicroCMS.Domain.Aggregates.Tenant;

/// <summary>
/// Per-site configuration aggregate (GAP-18).
/// Owns feature flags, preview URL template, CORS origins, and locale list.
/// Lives in a 1-to-1 relationship with <see cref="Site"/>.
/// </summary>
public sealed class SiteSettings : AggregateRoot<SiteId>
{
    private readonly List<string> _corsOrigins = [];
    private readonly List<string> _locales = [];
    private readonly List<ConfigEntry> _configEntries = [];

    private SiteSettings() : base() { } // EF Core

    private SiteSettings(SiteId siteId, TenantId tenantId) : base(siteId)
  {
        TenantId = tenantId;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

  public TenantId TenantId { get; private set; }
    public string? PreviewUrlTemplate { get; private set; }
    public bool VersioningEnabled { get; private set; } = true;
    public bool WorkflowEnabled { get; private set; } = true;
    public bool SchedulingEnabled { get; private set; } = true;
    public bool PreviewEnabled { get; private set; } = true;
    public bool AiEnabled { get; private set; } = true;
    public DateTimeOffset UpdatedAt { get; private set; }
    public IReadOnlyList<string> CorsOrigins => _corsOrigins.AsReadOnly();
    public IReadOnlyList<string> Locales => _locales.AsReadOnly();

    /// <summary>Site-level key-value configuration overrides (GAP-AI-1).</summary>
    public IReadOnlyList<ConfigEntry> ConfigEntries => _configEntries.AsReadOnly();

    // ── Factory ────────────────────────────────────────────────────────────

    public static SiteSettings CreateDefault(SiteId siteId, TenantId tenantId, Locale defaultLocale)
    {
 var settings = new SiteSettings(siteId, tenantId);
    settings._locales.Add(defaultLocale.Value);
        return settings;
    }

    // ── Mutations ─────────────────────────────────────────────────────────

    public void UpdateFeatureFlags(
        bool versioning, bool workflow, bool scheduling, bool preview, bool ai)
    {
 VersioningEnabled = versioning;
     WorkflowEnabled = workflow;
        SchedulingEnabled = scheduling;
        PreviewEnabled = preview;
        AiEnabled = ai;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void SetPreviewUrlTemplate(string? template)
    {
        PreviewUrlTemplate = template?.Trim();
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void SetCorsOrigins(IEnumerable<string> origins)
    {
        _corsOrigins.Clear();
        foreach (var o in origins.Select(x => x.Trim()).Where(x => x.Length > 0).Distinct())
        {
        if (!Uri.TryCreate(o, UriKind.Absolute, out _))
   throw new DomainException($"CORS origin '{o}' is not a valid absolute URL.");
       _corsOrigins.Add(o);
        }
   UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void SetLocales(IEnumerable<string> localeCodes)
    {
   var list = localeCodes.Select(c => c.Trim()).Where(c => c.Length > 0).Distinct().ToList();
        if (list.Count == 0)
            throw new DomainException("A site must support at least one locale.");
             _locales.Clear();
             _locales.AddRange(list);
         UpdatedAt = DateTimeOffset.UtcNow;
         }

         /// <summary>Inserts or replaces the site-level config entry for <paramref name="key"/>.</summary>
         public void UpsertEntry(string key, string value, string category = "general", bool isSecret = false)
         {
             ArgumentException.ThrowIfNullOrWhiteSpace(key, nameof(key));

             var existing = _configEntries.Find(e => e.Key == key);
             if (existing is not null)
             {
                 existing.Update(value, category, isSecret);
             }
             else
             {
                 _configEntries.Add(ConfigEntry.Create(key, value, category, isSecret));
             }

             UpdatedAt = DateTimeOffset.UtcNow;
         }

         /// <summary>Removes the site-level config entry for <paramref name="key"/>. No-op if absent.</summary>
         public void RemoveEntry(string key)
         {
             ArgumentException.ThrowIfNullOrWhiteSpace(key, nameof(key));
             _configEntries.RemoveAll(e => e.Key == key);
             UpdatedAt = DateTimeOffset.UtcNow;
         }

         /// <summary>Returns the site-level config entry for <paramref name="key"/>, or <c>null</c>.</summary>
         public ConfigEntry? GetEntry(string key) =>
             _configEntries.Find(e => e.Key == key);
     }
