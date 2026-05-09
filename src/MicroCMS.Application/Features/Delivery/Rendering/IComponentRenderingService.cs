using MicroCMS.Domain.Aggregates.Components;
using MicroCMS.Domain.Aggregates.Content;

namespace MicroCMS.Application.Features.Delivery.Rendering;

/// <summary>
/// Application-layer abstraction over the concrete rendering engines
/// (<c>IComponentRenderer</c> and <c>ILayoutRenderer</c>) in <c>MicroCMS.Delivery.Core</c>.
///
/// Registered by <c>AddDeliveryServices()</c> in the Delivery composition root.
/// In non-delivery hosts (e.g. admin WebHost) this can be registered as a no-op stub.
/// </summary>
public interface IComponentRenderingService
{
    /// <summary>Renders a single component item to an HTML fragment.</summary>
    Task<string> RenderComponentAsync(
        Component component,
        Entry item,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Renders a component using only its template, with no entry data (empty field bag).
    /// Used for static/structural components that carry no backing content type.
    /// </summary>
    Task<string> RenderComponentStaticAsync(
        Component component,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Injects zone HTML into a layout shell, resolves all <c>{{namespace:key}}</c> tokens
    /// via the registered <see cref="TokenResolutionPipeline"/>, and returns a full HTML document.
    /// </summary>
    Task<string> RenderLayoutAsync(
        Layout layout,
        IReadOnlyDictionary<string, string> zones,
        RenderContext renderContext,
        string? seoTitle       = null,
        string? seoDescription = null,
        string? seoOgImage     = null,
        CancellationToken cancellationToken = default);
}
