using MicroCMS.Domain.Exceptions;
using MicroCMS.Shared.Ids;

namespace MicroCMS.Domain.Aggregates.Components;

/// <summary>
/// The rendering engine used for a <see cref="Layout"/> shell.
/// Mirrors <see cref="RenderingTemplateType"/> but scoped to whole-page layouts.
/// </summary>
public enum LayoutTemplateType
{
    /// <summary>ASP.NET Core Razor layout (_Layout.cshtml). Default.</summary>
    Razor = 0,

    /// <summary>Handlebars template — zones injected as named partials.</summary>
    Handlebars = 1,

    /// <summary>Raw HTML with <c>{{zone:name}}</c> placeholder tokens.</summary>
    Html = 2,
}

/// <summary>
/// Defines the master HTML shell for one or more pages.
///
/// A layout wraps rendered zone HTML inside a full document structure
/// (DOCTYPE, &lt;head&gt;, nav, footer, etc.).
/// Named zones in <see cref="ShellTemplate"/> are replaced at render time
/// by the accumulated HTML of all <see cref="ComponentPlacement"/> items
/// that belong to that zone.
///
/// Zone placeholder syntax (all template types):
///   <c>{{zone:hero-zone}}</c>
///   <c>{{zone:content-zone}}</c>
///   <c>{{zone:footer}}</c>
///
/// SEO placeholders (available in all template types):
///   <c>{{seo:title}}</c>  <c>{{seo:description}}</c>  <c>{{seo:ogImage}}</c>
/// </summary>
public sealed class Layout : AggregateRoot<LayoutId>
{
    public const int MaxNameLength = 200;
    public const int MaxKeyLength = 100;

    private Layout() : base() { } // EF Core

    private Layout(
        LayoutId id,
      TenantId tenantId,
 SiteId siteId,
        string name,
  string key,
        LayoutTemplateType templateType,
        string? shellTemplate)
        : base(id)
    {
        TenantId = tenantId;
        SiteId = siteId;
        Name = name;
        Key = key;
        TemplateType = templateType;
        ShellTemplate = shellTemplate;
        IsDefault = false;
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public TenantId TenantId { get; private set; }
    public SiteId SiteId { get; private set; }

    /// <summary>Human-readable name shown in the CMS, e.g. "Blog Layout".</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>
    /// Machine-readable handle used in templates and API responses, e.g. "blog-layout".
    /// Unique per site.
    /// </summary>
    public string Key { get; private set; } = string.Empty;

    /// <summary>Which engine renders <see cref="ShellTemplate"/>.</summary>
    public LayoutTemplateType TemplateType { get; private set; }

    /// <summary>
    /// The master shell template source.
    /// Must contain at least one <c>{{zone:*}}</c> placeholder.
    /// </summary>
    public string? ShellTemplate { get; private set; }

    /// <summary>
    /// When true this layout is used for pages that have no explicit layout assigned.
    /// Only one layout per site can be default.
    /// </summary>
    public bool IsDefault { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    // ── Factory ───────────────────────────────────────────────────────────

    public static Layout Create(
        TenantId tenantId,
        SiteId siteId,
        string name,
    string key,
        LayoutTemplateType templateType = LayoutTemplateType.Handlebars,
        string? shellTemplate = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name, nameof(name));
        ArgumentException.ThrowIfNullOrWhiteSpace(key, nameof(key));

        if (name.Length > MaxNameLength)
            throw new DomainException($"Layout name must not exceed {MaxNameLength} characters.");
        if (key.Length > MaxKeyLength)
            throw new DomainException($"Layout key must not exceed {MaxKeyLength} characters.");

        return new Layout(LayoutId.New(), tenantId, siteId,
                  name.Trim(), key.Trim().ToLowerInvariant(), templateType, shellTemplate?.Trim());
    }

    // ── Mutations ─────────────────────────────────────────────────────────

    public void Update(string name, LayoutTemplateType templateType, string? shellTemplate)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name, nameof(name));
        if (name.Length > MaxNameLength)
            throw new DomainException($"Layout name must not exceed {MaxNameLength} characters.");

        Name = name.Trim();
        TemplateType = templateType;
        ShellTemplate = shellTemplate?.Trim();
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Marks this layout as the site default.
    /// The caller (application layer) must clear <c>IsDefault</c> on any previous default
    /// within the same site before calling this, to maintain the single-default invariant.
    /// </summary>
    public void MarkAsDefault()
    {
        IsDefault = true;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void ClearDefault()
    {
        IsDefault = false;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
