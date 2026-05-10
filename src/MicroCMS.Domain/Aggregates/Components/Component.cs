using MicroCMS.Domain.Aggregates.Content;
using MicroCMS.Domain.Entities;
using MicroCMS.Domain.Events.Pages;
using MicroCMS.Domain.Exceptions;
using MicroCMS.Shared.Ids;

namespace MicroCMS.Domain.Aggregates.Components;

/// <summary>
/// The rendering engine used to render a component's template into HTML.
/// Matches the options shown in the component editor "Rendering Template" select.
/// </summary>
public enum RenderingTemplateType
{
    /// <summary>
    /// Handlebars template (.hbs) — fully rendered server-side via Handlebars.Net.
    /// Fields are flattened to named tokens: <c>{{heading}}</c>, <c>{{{body}}}</c> (triple-stash for raw HTML).
    /// Supports <c>{{#if}}</c>, <c>{{#each}}</c>, partials and helpers.
    /// <b>This is the recommended default for server-side rendering.</b>
    /// </summary>
    Handlebars = 0,

  /// <summary>
    /// React component (.tsx) — <c>TemplateContent</c> is not rendered server-side.
    /// The Delivery API emits a <c>&lt;!-- component:key id:... type:React --&gt;</c>
    /// hydration hint; the frontend is responsible for mounting the component.
  /// </summary>
    React = 1,

  /// <summary>
    /// Web Component / Custom Element — rendered entirely client-side.
    /// The Delivery API emits a hydration hint comment; no server rendering occurs.
    /// </summary>
WebComponent = 2,

    /// <summary>
    /// ASP.NET Core Razor partial (.cshtml) — <c>TemplateContent</c> is not rendered server-side
/// by the Delivery API (which is a non-MVC host and has no Razor view engine).
    /// Use this type only when the component partial lives on disk inside an MVC host
    /// and is rendered via <c>Html.PartialAsync(component.Key)</c> in that host.
    /// The Delivery API emits a hydration hint comment for this type.
    /// </summary>
    RazorPartial = 3,

    /// <summary>
    /// Plain HTML template — <c>TemplateContent</c> is returned as-is after substituting
    /// <c>{{fieldName}}</c> tokens with the corresponding item field values.
    /// No Handlebars helpers or partials are evaluated.
    /// </summary>
    Html = 4,
}

/// <summary>
/// Defines a reusable UI component schema (GAP-22).
/// A Component is the template (schema + rendering template).
/// ComponentItems are the concrete data instances of that component.
/// </summary>
public sealed class Component : AggregateRoot<ComponentId>
{
    public const int MaxNameLength = 200;
    public const int MaxKeyLength = 100;
    public const int MaxDescriptionLength = 500;
    public const int MaxCategoryLength = 50;

    private Component() : base() { } // EF Core

    private Component(
        ComponentId id,
        TenantId tenantId,
        SiteId siteId,
        string name,
      string key,
        string? description,
  string category)
  : base(id)
    {
    TenantId = tenantId;
SiteId = siteId;
        Name = name;
        Key = key;
        Description = description;
        Category = category;
        TemplateType = RenderingTemplateType.Handlebars;
      CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public TenantId TenantId { get; private set; }
    public SiteId SiteId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Key { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public string Category { get; private set; } = "Content";
    // ZonesJson kept as a DB column for backward compat; no longer part of the public API.
    private string ZonesJson { get; set; } = "[]";
    public int UsageCount { get; private set; }
    public int ItemCount { get; private set; }
    public RenderingTemplateType TemplateType { get; private set; } = RenderingTemplateType.Handlebars;
    public string? TemplateContent { get; private set; }

    /// <summary>
    /// Optional wireframe / thumbnail image stored as a data-URI (SVG or raster).
    /// Displayed in the component library browser and page-designer palette.
    /// </summary>
    public string? ThumbnailDataUri { get; private set; }

    /// <summary>
    /// The auto-created <see cref="ContentType"/> that stores field data for items of this component.
    /// Set once during component creation by <c>ComponentBackingTypeProvisioner</c>.
    /// Never null after provisioning completes.
    /// </summary>
    public ContentTypeId? BackingContentTypeId { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    // ── Factory ────────────────────────────────────────────────────────────

    public static Component Create(
        TenantId tenantId,
     SiteId siteId,
      string name,
        string key,
        string? description,
        string category)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name, nameof(name));
        ArgumentException.ThrowIfNullOrWhiteSpace(key, nameof(key));
        if (name.Length > MaxNameLength)
      throw new DomainException($"Component name must not exceed {MaxNameLength} characters.");
        if (key.Length > MaxKeyLength)
            throw new DomainException($"Component key must not exceed {MaxKeyLength} characters.");

 return new Component(ComponentId.New(), tenantId, siteId, name.Trim(), key.Trim(),
            description?.Trim(), category.Trim());
    }

    // ── Mutations ──────────────────────────────────────────────────────────

    public void Update(string name, string? description, string category)
    {
    ArgumentException.ThrowIfNullOrWhiteSpace(name, nameof(name));
        if (name.Length > MaxNameLength)
    throw new DomainException($"Component name must not exceed {MaxNameLength} characters.");
        Name = name.Trim();
 Description = description?.Trim();
        Category = category.Trim();
     UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>Saves (creates or replaces) the rendering template for this component.</summary>
    public void UpdateTemplate(RenderingTemplateType templateType, string? templateContent)
    {
    TemplateType = templateType;
        TemplateContent = templateContent?.Trim();
        UpdatedAt = DateTimeOffset.UtcNow;
        RaiseDomainEvent(new ComponentUpdatedEvent(Id, TenantId, SiteId));
    }

    /// <summary>Saves or clears the wireframe thumbnail for this component.</summary>
    public void UpdateThumbnail(string? thumbnailDataUri)
    {
        ThumbnailDataUri = string.IsNullOrWhiteSpace(thumbnailDataUri) ? null : thumbnailDataUri.Trim();
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Called once by <c>ComponentBackingTypeProvisioner</c> after the backing ContentType is created.
    /// </summary>
    public void SetBackingContentType(ContentTypeId contentTypeId)
    {
      if (BackingContentTypeId is not null)
            throw new BusinessRuleViolationException(
          "Component.BackingTypeAlreadySet",
          "Backing ContentType has already been assigned to this component.");
        BackingContentTypeId = contentTypeId;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void IncrementUsage() { UsageCount++; UpdatedAt = DateTimeOffset.UtcNow; }
    public void DecrementUsage() { UsageCount = Math.Max(0, UsageCount - 1); UpdatedAt = DateTimeOffset.UtcNow; }
    public void IncrementItemCount() { ItemCount++; UpdatedAt = DateTimeOffset.UtcNow; }
    public void DecrementItemCount() { ItemCount = Math.Max(0, ItemCount - 1); UpdatedAt = DateTimeOffset.UtcNow; }
}
