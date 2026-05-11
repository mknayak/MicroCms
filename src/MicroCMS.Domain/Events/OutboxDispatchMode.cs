namespace MicroCMS.Domain.Events;

/// <summary>
/// Controls how an outbox message is dispatched when multiple host instances share the same database.
/// </summary>
public enum OutboxDispatchMode
{
    /// <summary>
    /// Default. Exactly one host instance processes the message and marks it done.
    /// Appropriate for side-effects that must happen once: sending a webhook, writing to a search
    /// index, charging a quota, etc.
    /// </summary>
    Exclusive = 0,

    /// <summary>
    /// Every registered host instance must process the message independently.
    /// Appropriate for local in-process state that differs per instance: in-memory cache
    /// invalidation, local file purge, per-process counters, etc.
    /// Each instance records its own <c>OutboxDeliveryRecord</c> row so progress is tracked
    /// separately and a crashed instance can replay on restart.
    /// </summary>
    Broadcast = 1,
}
