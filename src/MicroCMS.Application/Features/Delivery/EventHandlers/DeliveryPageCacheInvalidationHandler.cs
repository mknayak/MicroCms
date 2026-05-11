using MediatR;
using MicroCMS.Application.Common.Events;
using MicroCMS.Application.Common.Interfaces;
using MicroCMS.Application.Features.Delivery.Caching;
using MicroCMS.Domain.Aggregates.Content;
using MicroCMS.Domain.Aggregates.Pages;
using MicroCMS.Domain.Events.Content;
using MicroCMS.Domain.Events.Pages;
using MicroCMS.Domain.Repositories;
using MicroCMS.Domain.Specifications.Delivery;
using MicroCMS.Domain.ValueObjects;
using MicroCMS.Shared.Ids;
using Microsoft.Extensions.Logging;

namespace MicroCMS.Application.Features.Delivery.EventHandlers;

/// <summary>
/// Invalidates the rendered-page cache when content or structural changes are made in Authoring.
///
/// This handler is subscribed via the transactional outbox: Authoring's
/// <c>OutboxDispatcherJob</c> dispatches domain events as MediatR notifications,
/// which arrive here asynchronously after the authoring transaction commits.
/// Delivery stays fully read-only — it just gets a cache miss on the next request.
///
/// Invalidation strategy:
///   • PageUpdatedEvent        → remove the single affected page key
///   • EntryPublishedEvent     → find pages linked to the entry and remove their keys
///   • EntryUnpublishedEvent   → same as above
///   • EntryUpdatedEvent       → same as above (draft-save should invalidate preview too)
///   • LayoutUpdatedEvent      → remove all rendered pages for the site (bulk tag eviction)
///   • ComponentUpdatedEvent   → remove all rendered pages for the site (bulk tag eviction)
///   • SiteTemplateUpdatedEvent→ remove all rendered pages for the site (bulk tag eviction)
/// </summary>
internal sealed class DeliveryPageCacheInvalidationHandler(
    IRepository<Page, PageId> pageRepo,
    ICacheService cache,
    ILogger<DeliveryPageCacheInvalidationHandler> logger)
    : INotificationHandler<DomainEventNotification<PageUpdatedEvent>>,
      INotificationHandler<DomainEventNotification<EntryPublishedEvent>>,
      INotificationHandler<DomainEventNotification<EntryUnpublishedEvent>>,
      INotificationHandler<DomainEventNotification<EntryUpdatedEvent>>,
      INotificationHandler<DomainEventNotification<LayoutUpdatedEvent>>,
      INotificationHandler<DomainEventNotification<ComponentUpdatedEvent>>,
      INotificationHandler<DomainEventNotification<SiteTemplateUpdatedEvent>>
{
    // ── Page changed directly ─────────────────────────────────────────────

    public Task Handle(
        DomainEventNotification<PageUpdatedEvent> notification,
        CancellationToken cancellationToken)
    {
        var ev = notification.DomainEvent;
        return InvalidatePageAsync(ev.SiteId.Value, ev.Slug, cancellationToken);
    }

    // ── Entry publish/unpublish/update → find linked pages ────────────────

    public Task Handle(
        DomainEventNotification<EntryPublishedEvent> notification,
        CancellationToken cancellationToken)
        => InvalidatePagesForEntryAsync(notification.DomainEvent.SiteId, notification.DomainEvent.EntryId, cancellationToken);

    public Task Handle(
        DomainEventNotification<EntryUnpublishedEvent> notification,
        CancellationToken cancellationToken)
        => InvalidatePagesForEntryAsync(siteId: null, notification.DomainEvent.EntryId, cancellationToken);

    public Task Handle(
        DomainEventNotification<EntryUpdatedEvent> notification,
        CancellationToken cancellationToken)
    {
        // EntryUpdatedEvent does not carry SiteId — resolve pages by EntryId across all sites.
        return InvalidatePagesForEntryAsync(siteId: null, notification.DomainEvent.EntryId, cancellationToken);
    }

    // ── Site-wide structural changes → bulk eviction ──────────────────────

    public Task Handle(
        DomainEventNotification<LayoutUpdatedEvent> notification,
        CancellationToken cancellationToken)
        => InvalidateSiteAsync(notification.DomainEvent.SiteId.Value, cancellationToken);

    public Task Handle(
        DomainEventNotification<ComponentUpdatedEvent> notification,
        CancellationToken cancellationToken)
        => InvalidateSiteAsync(notification.DomainEvent.SiteId.Value, cancellationToken);

    public Task Handle(
        DomainEventNotification<SiteTemplateUpdatedEvent> notification,
        CancellationToken cancellationToken)
        => InvalidateSiteAsync(notification.DomainEvent.SiteId.Value, cancellationToken);

    // ── Helpers ───────────────────────────────────────────────────────────

    private async Task InvalidatePageAsync(Guid siteId, string slug, CancellationToken ct)
    {
        try
        {
            var key = DeliveryCacheKeys.RenderedPage(siteId, slug);
            await cache.RemoveAsync(key, ct);
            logger.LogDebug(
                "DeliveryCache: evicted page key {Key} (site={SiteId}, slug={Slug}).",
                key, siteId, slug);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "DeliveryCache: failed to evict page key for slug={Slug}.", slug);
        }
    }

    private async Task InvalidateSiteAsync(Guid siteId, CancellationToken ct)
    {
        try
        {
            var tag = DeliveryCacheKeys.SiteTag(siteId);
            await cache.RemoveByTagAsync(tag, ct);
            logger.LogDebug(
                "DeliveryCache: bulk-evicted all pages for site {SiteId} via tag {Tag}.",
                siteId, tag);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "DeliveryCache: failed bulk eviction for site {SiteId}.", siteId);
        }
    }

    private async Task InvalidatePagesForEntryAsync(SiteId? siteId, EntryId entryId, CancellationToken ct)
    {
        try
        {
            IReadOnlyList<Page> linkedPages;
            if (siteId is not null)
            {
                linkedPages = await pageRepo.ListAsync(
                    new PagesByLinkedEntrySpec(siteId.Value, entryId), ct);
            }
            else
            {
                linkedPages = await pageRepo.ListAsync(
                    new PagesByLinkedEntrySpec(entryId), ct);
            }

            foreach (var page in linkedPages)
                await InvalidatePageAsync(page.SiteId.Value, page.Slug.Value, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "DeliveryCache: failed to evict pages for entry {EntryId}.", entryId);
        }
    }
}
