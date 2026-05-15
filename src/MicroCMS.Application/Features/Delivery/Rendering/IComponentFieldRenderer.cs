using MicroCMS.Shared.Ids;

namespace MicroCMS.Application.Features.Delivery.Rendering;

/// <summary>
/// Renders a <see cref="MicroCMS.Domain.Enums.FieldType.Component"/> field value
/// (a single component-item entry GUID) into a final HTML string by invoking the
/// owning component's template engine.
///
/// Registered in the Delivery host; a no-op null is used in non-delivery hosts
/// (admin WebHost) so that <see cref="Services.EntryFieldExpander"/> can still
/// fall back to reference-style JSON expansion when the renderer is unavailable.
/// </summary>
public interface IComponentFieldRenderer
{
    /// <summary>
    /// Renders the component item identified by <paramref name="itemEntryId"/>
    /// using the template belonging to the component identified by <paramref name="componentKey"/>.
    /// </summary>
    /// <param name="componentKey">The component's machine-readable key (e.g. "button").</param>
    /// <param name="itemEntryId">The entry GUID that holds the component item's field data.</param>
    /// <param name="siteId">Site context used to scope the component/entry lookup.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// The rendered HTML fragment, or <c>null</c> if the component or item cannot be found.
    /// </returns>
    Task<string?> RenderAsync(
        string componentKey,
        Guid itemEntryId,
        SiteId siteId,
        CancellationToken ct = default);
}
