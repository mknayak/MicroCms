using HotChocolate.Subscriptions;
using MediatR;
using MicroCMS.Application.Common.Events;
using DomainEntryPublishedEvent = MicroCMS.Domain.Events.Content.EntryPublishedEvent;

namespace MicroCMS.GraphQL.Subscriptions;

/// <summary>
/// MediatR notification handler that forwards <see cref="DomainEntryPublishedEvent"/> domain events
/// to the Hot Chocolate in-process subscription topic, bridging the domain event pipeline
/// with GraphQL WebSocket subscriptions.
/// </summary>
internal sealed class EntryPublishedSubscriptionBridge(ITopicEventSender sender)
    : INotificationHandler<DomainEventNotification<DomainEntryPublishedEvent>>
{
  public async Task Handle(
    DomainEventNotification<DomainEntryPublishedEvent> notification,
        CancellationToken cancellationToken)
    {
        var domainEvent = notification.DomainEvent;

     var graphQlEvent = new EntryPublishedEvent(
            domainEvent.EntryId.Value,
            domainEvent.TenantId.Value,
        domainEvent.SiteId.Value,
        domainEvent.Slug,
          domainEvent.Locale,
            DateTimeOffset.UtcNow);

  await sender.SendAsync(
          EntrySubscriptionTopics.EntryPublished,
          graphQlEvent,
     cancellationToken);
    }
}
