using System.Linq;

namespace MicroCMS.Domain.Events;

/// <summary>
/// Declares how the outbox dispatcher should deliver a domain event when multiple host instances
/// share the same database.
///
/// Apply this attribute to a domain event record class to override the default mode.
/// When the attribute is absent the event is treated as <see cref="OutboxDispatchMode.Exclusive"/>.
///
/// <example>
/// <code>
/// // Delivered to every running instance (cache invalidation, per-process state):
/// [OutboxDispatch(OutboxDispatchMode.Broadcast)]
/// public sealed record PageUpdatedEvent(...) : DomainEvent;
///
/// // Delivered once by whichever instance claims it first (webhooks, search index):
/// [OutboxDispatch(OutboxDispatchMode.Exclusive)]   // or omit — Exclusive is the default
/// public sealed record EntryPublishedEvent(...) : DomainEvent;
/// </code>
/// </example>
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class OutboxDispatchAttribute(OutboxDispatchMode mode) : Attribute
{
    public OutboxDispatchMode Mode { get; } = mode;

    /// <summary>
    /// Returns the <see cref="OutboxDispatchMode"/> declared on <paramref name="eventType"/>,
    /// or <see cref="OutboxDispatchMode.Exclusive"/> when the attribute is absent.
    /// </summary>
    public static OutboxDispatchMode For(Type eventType)
    {
        var attr = eventType.GetCustomAttributes(typeof(OutboxDispatchAttribute), inherit: false)
                            .OfType<OutboxDispatchAttribute>()
                            .FirstOrDefault();
        return attr?.Mode ?? OutboxDispatchMode.Exclusive;
    }
}
