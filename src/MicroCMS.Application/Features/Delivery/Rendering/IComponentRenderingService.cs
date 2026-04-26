using MicroCMS.Domain.Aggregates.Components;

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
        ComponentItem item,
      CancellationToken cancellationToken = default);

    /// <summary>Injects zone HTML into a layout shell and returns a full HTML document.</summary>
    Task<string> RenderLayoutAsync(
   Layout layout,
   IReadOnlyDictionary<string, string> zones,
 string? seoTitle       = null,
        string? seoDescription = null,
     string? seoOgImage     = null,
        CancellationToken cancellationToken = default);
}
