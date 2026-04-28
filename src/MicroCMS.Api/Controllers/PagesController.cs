using MicroCMS.Application.Features.Pages.Commands;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MicroCMS.Api.Controllers;

/// <summary>Page tree CRUD, layout assignment, SEO, and PageTemplate (zone) management (GAP-21).</summary>
[Authorize]
public sealed class PagesController : ApiControllerBase
{
    // ── Page tree ─────────────────────────────────────────────────────────

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<PageTreeNode>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTree(
    [FromQuery] Guid siteId, CancellationToken ct = default) =>
   OkOrProblem(await Sender.Send(new GetSiteTreeQuery(siteId), ct));

    [HttpPost("static")]
    [ProducesResponseType(typeof(PageDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateStatic(
        [FromBody] CreateStaticPageCommand command, CancellationToken ct = default)
    {
        var result = await Sender.Send(command, ct);
        return CreatedOrProblem(result, nameof(GetTree), new { siteId = command.SiteId });
    }

    [HttpPost("collection")]
    [ProducesResponseType(typeof(PageDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateCollection(
        [FromBody] CreateCollectionPageCommand command, CancellationToken ct = default)
    {
        var result = await Sender.Send(command, ct);
        return CreatedOrProblem(result, nameof(GetTree), new { siteId = command.SiteId });
    }

    [HttpPut("{id:guid}/move")]
    [ProducesResponseType(typeof(PageDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Move(
        Guid id, [FromBody] MovePageRequest r, CancellationToken ct = default) =>
        OkOrProblem(await Sender.Send(new MovePageCommand(id, r.NewParentId), ct));

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct = default) =>
        NoContentOrProblem(await Sender.Send(new DeletePageCommand(id), ct));

    // ── Layout assignment ─────────────────────────────────────────────────

    /// <summary>
    /// Assigns or clears the layout for a page.
    /// Pass <c>null</c> as <c>layoutId</c> to clear the override and use the site default layout.
    /// </summary>
    [HttpPut("{id:guid}/layout")]
    [ProducesResponseType(typeof(PageDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetLayout(
            Guid id, [FromBody] SetPageLayoutRequest r, CancellationToken ct = default) =>
         OkOrProblem(await Sender.Send(new SetPageLayoutCommand(id, r.LayoutId), ct));

    // ── Site template assignment ───────────────────────────────────────────

    /// <summary>
    /// Assigns or clears the site template this page inherits component placements from.
    /// Pass <c>null</c> as <c>siteTemplateId</c> to clear the link (page-specific placements only).
    /// </summary>
    [HttpPut("{id:guid}/site-template")]
    [ProducesResponseType(typeof(PageDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetSiteTemplate(
      Guid id, [FromBody] SetPageSiteTemplateRequest r, CancellationToken ct = default) =>
  OkOrProblem(await Sender.Send(new SetPageSiteTemplateCommand(id, r.SiteTemplateId), ct));

    // ── PageTemplate (zone placements) ────────────────────────────────────

    /// <summary>
    /// Returns the PageTemplate for a page — the zone map with ordered ComponentPlacements.
    /// Returns 404 if no template has been configured yet.
    /// </summary>
    [HttpGet("{id:guid}/template")]
    [ProducesResponseType(typeof(PageTemplateDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTemplate(Guid id, CancellationToken ct = default) =>
      OkOrProblem(await Sender.Send(new GetPageTemplateQuery(id), ct));

    /// <summary>
    /// Saves (creates or fully replaces) the PageTemplate for a page.
    ///
    /// Send an ordered list of placements. Each placement specifies:
    /// <ul>
    ///   <li><c>componentId</c> — the component whose published items will be rendered.</li>
    ///   <li><c>zone</c> — the Layout zone name it renders into, e.g. <c>"hero-zone"</c>.</li>
    ///   <li><c>sortOrder</c> — render order within the zone (lower = first).</li>
    /// </ul>
    /// All existing placements are replaced atomically.
    /// </summary>
    [HttpPut("{id:guid}/template")]
    [ProducesResponseType(typeof(PageTemplateDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> SaveTemplate(
        Guid id, [FromBody] SavePageTemplateRequest r, CancellationToken ct = default) =>
        OkOrProblem(await Sender.Send(new SavePageTemplateCommand(id, r.Placements), ct));

    // ── SEO ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Sets page-level SEO metadata.
    /// These values are injected into <c>{{seo:title}}</c>, <c>{{seo:description}}</c>,
    /// and <c>{{seo:ogImage}}</c> layout tokens at render time.
    /// Pass <c>null</c> for any field to clear it and fall back to the linked entry's SEO.
    /// </summary>
    [HttpPut("{id:guid}/seo")]
    [ProducesResponseType(typeof(PageDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetSeo(
        Guid id, [FromBody] SetPageSeoRequest r, CancellationToken ct = default) =>
        OkOrProblem(await Sender.Send(
            new UpdatePageSeoCommand(id, r.MetaTitle, r.MetaDescription, r.CanonicalUrl, r.OgImage), ct));

    // ── Page detail ───────────────────────────────────────────────────────

    /// <summary>Returns the full PageDto for a single page (includes linkedEntryId, seo, etc.).</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(PageDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct = default) =>
        OkOrProblem(await Sender.Send(new GetPageQuery(id), ct));

    // ── Entry link ────────────────────────────────────────────────────────

  /// <summary>
    /// Links or clears the entry associated with a Static page.
 /// Pass <c>null</c> as <c>entryId</c> to clear the association.
    /// </summary>
    [HttpPut("{id:guid}/entry")]
    [ProducesResponseType(typeof(PageDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> SetLinkedEntry(
        Guid id, [FromBody] SetPageLinkedEntryRequest r, CancellationToken ct = default) =>
        OkOrProblem(await Sender.Send(new LinkPageEntryCommand(id, r.EntryId), ct));
}

// ── Request types ──────────────────────────────────────────────────────────────

public sealed record MovePageRequest(Guid? NewParentId);

public sealed record SetPageLayoutRequest(Guid? LayoutId);

public sealed record SavePageTemplateRequest(
    IReadOnlyList<PageTemplatePlacementInput> Placements);

public sealed record SetPageSeoRequest(
    string? MetaTitle,
    string? MetaDescription,
    string? CanonicalUrl,
    string? OgImage);

public sealed record SetPageLinkedEntryRequest(Guid? EntryId);

// ── New: site-template assignment body ────────────────────────────────────────
public sealed record SetPageSiteTemplateRequest(Guid? SiteTemplateId);
