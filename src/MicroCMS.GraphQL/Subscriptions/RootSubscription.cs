using HotChocolate;
using HotChocolate.Execution;
using HotChocolate.Subscriptions;
using HotChocolate.Types;
using MicroCMS.Application.Features.Entries.Dtos;
using MicroCMS.GraphQL.Subscriptions;

namespace MicroCMS.GraphQL.Subscriptions;

/// <summary>Root GraphQL subscription type.</summary>
[GraphQLName("Subscription")]
public sealed class RootSubscription
{
 /// <summary>
  /// Fires whenever an entry transitions to Published status.
    /// Clients can optionally filter by <paramref name="siteId"/>.
    /// </summary>
  [Subscribe]
    [Topic(EntrySubscriptionTopics.EntryPublished)]
    public EntryPublishedEvent OnEntryPublished(
        [EventMessage] EntryPublishedEvent payload,
        Guid? siteId) =>
        // Runtime filter: client receives the event only when no siteId filter is set
        // or when the event matches the requested site.
siteId == null || payload.SiteId == siteId ? payload : null!;
}

/// <summary>Subscription event message for <see cref="RootSubscription.OnEntryPublished"/>.</summary>
public sealed record EntryPublishedEvent(
    Guid EntryId,
    Guid TenantId,
    Guid SiteId,
 string Slug,
    string Locale,
    DateTimeOffset PublishedAt);

/// <summary>Well-known topic names for Hot Chocolate in-memory subscriptions.</summary>
public static class EntrySubscriptionTopics
{
    public const string EntryPublished = "entry_published";
}
