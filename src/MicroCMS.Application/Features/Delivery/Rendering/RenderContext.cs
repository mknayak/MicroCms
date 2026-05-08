using MicroCMS.Shared.Ids;

namespace MicroCMS.Application.Features.Delivery.Rendering;

/// <summary>
/// Carries all per-request data that token resolvers need at render time.
///
/// Passed from the query handler into <see cref="IComponentRenderingService.RenderLayoutAsync"/>
/// and forwarded through to each <see cref="ITokenResolver"/> in the pipeline.
/// </summary>
public sealed class RenderContext
{
    /// <summary>Site that owns the page being rendered.</summary>
    public required SiteId SiteId { get; init; }

    /// <summary>Tenant that owns the site.</summary>
    public required TenantId TenantId { get; init; }

    // ── page:* namespace ─────────────────────────────────────────────────

    /// <summary>URL slug of the page being rendered (e.g. <c>about-us</c>).</summary>
    public required string PageSlug { get; init; }

    /// <summary>Display title of the page.</summary>
    public required string PageTitle { get; init; }

    /// <summary>ISO-8601 publish date of the page, or null if unpublished/draft.</summary>
    public DateTimeOffset? PagePublishedAt { get; init; }

    /// <summary>
    /// Fields from the page's linked entry (i.e. <c>Page.LinkedEntryId</c>),
    /// flattened to string values. Exposed as <c>page:{fieldName}</c> tokens.
    /// Empty when the page has no linked entry.
    /// </summary>
    public IReadOnlyDictionary<string, string> PageFields { get; init; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    // ── template:* namespace

    /// <summary>
    /// Machine key of the layout being used (e.g. <c>blog-post</c>), or null.
    /// Exposed as <c>template:key</c>.
    /// </summary>
    public string? TemplateKey { get; init; }

    /// <summary>
    /// Human-readable name of the layout, or null.
    /// Exposed as <c>template:name</c>.
    /// </summary>
    public string? TemplateName { get; init; }

    // ── user:* namespace (request-scoped — never cached) ─────────────────

    /// <summary>
    /// Authenticated user's ID if the delivery request is authenticated, otherwise null.
    /// </summary>
    public string? UserId { get; init; }

    /// <summary>Authenticated user's display name, or null.</summary>
    public string? UserName { get; init; }

    /// <summary>Authenticated user's primary role, or null.</summary>
    public string? UserRole { get; init; }
}
