using System.Text.Json;
using MediatR;
using MicroCMS.Application.Common.Events;
using MicroCMS.Application.Common.Interfaces;
using MicroCMS.Domain.Aggregates.Content;
using MicroCMS.Domain.Events.Content;
using MicroCMS.Domain.Repositories;
using MicroCMS.Shared.Ids;
using Microsoft.Extensions.Logging;

namespace MicroCMS.Application.Features.Search.EventHandlers;

/// <summary>
/// Sprint 9 — indexes an entry in the search index when it is published, updated, or archived;
/// removes it when unpublished or archived. Cache invalidation by tag is also performed.
/// Handlers are fire-and-forget: errors are logged and swallowed so a broken search backend
/// does not block authoring.
///
/// Domain events are delivered to this handler via <see cref="DomainEventNotification{T}"/>
/// which wraps the raw <see cref="IDomainEvent"/> in a MediatR <see cref="INotification"/> —
/// keeping the Domain layer free of the MediatR dependency.
/// </summary>
internal sealed class EntrySearchIndexerEventHandler(
    IRepository<Entry, EntryId> entryRepository,
    ISearchService searchService,
    ICacheService cacheService,
    ILogger<EntrySearchIndexerEventHandler> logger)
    : INotificationHandler<DomainEventNotification<EntryPublishedEvent>>,
      INotificationHandler<DomainEventNotification<EntryUnpublishedEvent>>,
      INotificationHandler<DomainEventNotification<EntryUpdatedEvent>>,
      INotificationHandler<DomainEventNotification<EntryArchivedEvent>>
{
    public Task Handle(DomainEventNotification<EntryPublishedEvent> notification, CancellationToken cancellationToken)
        => IndexAsync(notification.DomainEvent.EntryId, notification.DomainEvent.TenantId, cancellationToken);

    public Task Handle(DomainEventNotification<EntryUpdatedEvent> notification, CancellationToken cancellationToken)
        => IndexAsync(notification.DomainEvent.EntryId, notification.DomainEvent.TenantId, cancellationToken);

    public Task Handle(DomainEventNotification<EntryUnpublishedEvent> notification, CancellationToken cancellationToken)
        => SafeRemoveAsync(notification.DomainEvent.EntryId, notification.DomainEvent.TenantId, cancellationToken);

    public Task Handle(DomainEventNotification<EntryArchivedEvent> notification, CancellationToken cancellationToken)
        => SafeRemoveAsync(notification.DomainEvent.EntryId, notification.DomainEvent.TenantId, cancellationToken);

    private async Task IndexAsync(EntryId entryId, TenantId tenantId, CancellationToken ct)
    {
        try
        {
            var entry = await entryRepository.GetByIdAsync(entryId, ct);
            if (entry is null) return;

            var (title, excerpt, body, tags) = ExtractFields(entry.FieldsJson);

            var document = new SearchEntryDocument(
                entry.Id.Value,
                entry.TenantId.Value,
                entry.SiteId.Value,
                entry.ContentTypeId.Value,
                entry.Slug.Value,
                entry.Locale.Value,
                entry.Status.ToString(),
                title,
                excerpt,
                body,
                tags,
                entry.UpdatedAt,
                entry.PublishedAt);

            await searchService.IndexEntryAsync(document, ct);
            await InvalidateCacheAsync(tenantId, entryId, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to index entry {EntryId} in search.", entryId);
        }
    }

    private async Task SafeRemoveAsync(EntryId entryId, TenantId tenantId, CancellationToken ct)
    {
        try
        {
            await searchService.DeleteEntryAsync(tenantId, entryId.Value, ct);
            await InvalidateCacheAsync(tenantId, entryId, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to remove entry {EntryId} from search.", entryId);
        }
    }

    private async Task InvalidateCacheAsync(TenantId tenantId, EntryId entryId, CancellationToken ct)
    {
        await cacheService.RemoveAsync(CacheKeys.Entry(tenantId, entryId), ct);
        await cacheService.RemoveByTagAsync(CacheTags.TenantEntries(tenantId), ct);
    }

    // ── Helpers ──────────────────────────────────────────────────────────

    private static (string? title, string? excerpt, string? body, IReadOnlyList<string> tags)
        ExtractFields(string fieldsJson)
    {
        try
        {
            using var doc = JsonDocument.Parse(fieldsJson);
            var root = doc.RootElement;
            return (
                TryGetString(root, "title"),
                TryGetString(root, "excerpt"),
                TryGetString(root, "body"),
                TryGetTags(root));
        }
        catch (JsonException)
        {
            return (null, null, null, Array.Empty<string>());
        }
    }

    private static string? TryGetString(JsonElement root, string propertyName) =>
        root.TryGetProperty(propertyName, out var prop) && prop.ValueKind == JsonValueKind.String
            ? prop.GetString()
            : null;

    private static IReadOnlyList<string> TryGetTags(JsonElement root)
    {
        if (!root.TryGetProperty("tags", out var prop) || prop.ValueKind != JsonValueKind.Array)
            return Array.Empty<string>();

        var tags = new List<string>();
        foreach (var item in prop.EnumerateArray())
        {
            if (item.ValueKind == JsonValueKind.String && item.GetString() is { } s)
                tags.Add(s);
        }
        return tags;
    }
}

/// <summary>Centralised cache key builders — every key includes the tenantId (Sprint 9 / §18.4).</summary>
public static class CacheKeys
{
    public static string Entry(TenantId tenantId, EntryId entryId)
        => $"cms:{tenantId.Value}:entry:{entryId.Value}";

    public static string EntryList(
        TenantId tenantId, Guid siteId, string? status,
        Guid? contentTypeId, string? locale, Guid? folderId,
        int pageNumber, int pageSize)
        => $"cms:{tenantId.Value}:entries:{siteId}:{status ?? "all"}:{contentTypeId?.ToString() ?? "any"}:{locale ?? "any"}:{folderId?.ToString() ?? "any"}:{pageNumber}:{pageSize}";

    public static string ContentType(TenantId tenantId, Guid contentTypeId)
      => $"cms:{tenantId.Value}:contenttype:{contentTypeId}";

    public static string ContentTypeList(TenantId tenantId, Guid? siteId, int page, int pageSize)
        => $"cms:{tenantId.Value}:contenttypes:{siteId?.ToString() ?? "all"}:{page}:{pageSize}";
}

/// <summary>Centralised cache tag builders for bulk invalidation.</summary>
public static class CacheTags
{
    public static string TenantEntries(TenantId tenantId) => $"tenant:{tenantId.Value}:entries";
    public static string TenantContentTypes(TenantId tenantId) => $"tenant:{tenantId.Value}:contenttypes";
}
