using MicroCMS.Domain.Exceptions;
using MicroCMS.Shared.Ids;

namespace MicroCMS.Domain.Aggregates.Components;

/// <summary>
/// The rendering engine used for a <see cref="Layout"/> shell.
/// </summary>
public enum LayoutTemplateType
{
    /// <summary>
 /// Handlebars template (default).
    /// Zones are available as <c>{{{zone_hero_zone}}}</c> (triple-stash, unescaped HTML)
    /// or via the nested helper <c>{{{zones.[hero-zone]}}}</c>.
    /// Full Handlebars syntax is supported: <c>{{#if}}</c>, <c>{{#each}}</c>, partials, etc.
    /// </summary>
    Handlebars = 0,

    /// <summary>
    /// Raw HTML with <c>{{zone:name}}</c> and <c>{{seo:*}}</c> placeholder tokens.
    /// No logic constructs — pure string replacement.
    /// Use when the template is authored outside the CMS and pasted in as static HTML.
    /// </summary>
    Html = 1,
}

/// <summary>
/// Defines the master HTML shell, zone structure, and default component placements for pages.
/// <para>
/// Zones are stored as structured JSON (<see cref="ZonesJson"/>) and the
/// <see cref="ShellTemplate"/> is auto-generated from them by the Application layer
/// (<c>LayoutShellGeneratorService</c>). Direct edits to the shell template are not supported.
/// </para>
/// </summary>
public sealed class Layout : AggregateRoot<LayoutId>
{
    public const int MaxNameLength = 200;
    public const int MaxKeyLength = 100;

    private Layout() : base() { }

    private Layout(
        LayoutId id, TenantId tenantId, SiteId siteId,
        string name, string key, LayoutTemplateType templateType)
        : base(id)
    {
   TenantId = tenantId;
        SiteId = siteId;
        Name = name;
        Key = key;
        TemplateType = templateType;
        IsDefault = false;
        ZonesJson = BuildDefaultZonesJson();
        DefaultPlacementsJson = "[]";
      ShellTemplate = null;
        CreatedAt = DateTimeOffset.UtcNow;
    UpdatedAt = DateTimeOffset.UtcNow;
    }

    public TenantId TenantId { get; private set; }
    public SiteId SiteId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Key { get; private set; } = string.Empty;
    public LayoutTemplateType TemplateType { get; private set; }

    /// <summary>
    /// JSON array of <c>LayoutZoneNode</c> objects.
    /// Default layout ships with three zones: header, content, footer.
    /// </summary>
public string ZonesJson { get; private set; } = "[]";

    /// <summary>JSON array of <c>LayoutDefaultPlacement</c> objects pre-placed on every page.</summary>
    public string DefaultPlacementsJson { get; private set; } = "[]";

    /// <summary>
    /// Auto-generated HTML shell. Set by <c>LayoutShellGeneratorService</c> whenever
    /// <see cref="ZonesJson"/> changes. Never written directly by the UI.
    /// </summary>
    public string? ShellTemplate { get; private set; }

    public bool IsDefault { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    // ── Factory ───────────────────────────────────────────────────────────

public static Layout Create(
        TenantId tenantId, SiteId siteId,
  string name, string key,
        LayoutTemplateType templateType = LayoutTemplateType.Handlebars)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name, nameof(name));
        ArgumentException.ThrowIfNullOrWhiteSpace(key, nameof(key));
        if (name.Length > MaxNameLength)
   throw new DomainException($"Layout name must not exceed {MaxNameLength} characters.");
        if (key.Length > MaxKeyLength)
       throw new DomainException($"Layout key must not exceed {MaxKeyLength} characters.");

 return new Layout(LayoutId.New(), tenantId, siteId,
            name.Trim(), key.Trim().ToLowerInvariant(), templateType);
    }

    // ── Mutations ─────────────────────────────────────────────────────────

    public void Update(string name, LayoutTemplateType templateType)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name, nameof(name));
      if (name.Length > MaxNameLength)
  throw new DomainException($"Layout name must not exceed {MaxNameLength} characters.");
        Name = name.Trim();
        TemplateType = templateType;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
  /// Replaces the zone tree JSON. The Application layer must call
    /// <see cref="SetGeneratedShell"/> immediately after to keep the shell in sync.
/// </summary>
    public void UpdateZones(string zonesJson)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(zonesJson, nameof(zonesJson));
        ZonesJson = zonesJson;
  UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>Replaces default placement JSON for this layout.</summary>
    public void UpdateDefaultPlacements(string defaultPlacementsJson)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(defaultPlacementsJson, nameof(defaultPlacementsJson));
        DefaultPlacementsJson = defaultPlacementsJson;
    UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>Called by <c>LayoutShellGeneratorService</c> after zone changes.</summary>
    public void SetGeneratedShell(string shellTemplate)
    {
      ShellTemplate = shellTemplate;
     UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void MarkAsDefault() { IsDefault = true; UpdatedAt = DateTimeOffset.UtcNow; }
    public void ClearDefault() { IsDefault = false; UpdatedAt = DateTimeOffset.UtcNow; }

    // ── Helpers ───────────────────────────────────────────────────────────

    private static string BuildDefaultZonesJson() =>
        "[" +
 "{\"id\":\"zone-header\",\"type\":\"zone\",\"name\":\"header\",\"label\":\"Header\",\"sortOrder\":0}," +
        "{\"id\":\"zone-content\",\"type\":\"zone\",\"name\":\"content\",\"label\":\"Content\",\"sortOrder\":1}," +
        "{\"id\":\"zone-footer\",\"type\":\"zone\",\"name\":\"footer\",\"label\":\"Footer\",\"sortOrder\":2}" +
        "]";
}
