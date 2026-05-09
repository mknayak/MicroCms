namespace MicroCMS.Application.Features.SiteTemplates.Dtos;

/// <summary>Full detail DTO returned for a single site template.</summary>
public sealed record SiteTemplateDto(
    Guid Id,
    Guid TenantId,
    Guid SiteId,
    Guid LayoutId,
    string? LayoutName,
    string Name,
    string? Description,
    string PlacementsJson,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

/// <summary>Summary row used in list responses.</summary>
public sealed record SiteTemplateListItemDto(
    Guid Id,
    string Name,
    string? Description,
    Guid LayoutId,
    string LayoutName,
    int PageCount,
    DateTimeOffset UpdatedAt);

/// <summary>
/// The resolved effective SiteTemplate for a page, computed by walking the hierarchy:
///   1. Page.SiteTemplateId (explicit page override)
///   2. ContentType.SiteTemplateId (default for the page's content type)
///   3. None — caller falls back to Layout resolution (default or first layout for the site).
/// </summary>
public sealed record EffectiveTemplateDto(
    /// <summary>Which level supplied the template.</summary>
    EffectiveTemplateSource Source,
    /// <summary>Null when no SiteTemplate is configured at any level.</summary>
    SiteTemplateDto? Template);

public enum EffectiveTemplateSource
{
    /// <summary>No SiteTemplate found at any level.</summary>
    None,
    /// <summary>Template comes from the page's explicit SiteTemplate override.</summary>
    PageOverride,
    /// <summary>Template comes from the page's ContentType default SiteTemplate.</summary>
    ContentTypeDefault,
}
