namespace MicroCMS.Domain.Events;

/// <summary>
/// Marker interface for all domain events.
/// Domain events are dispatched by the infrastructure layer through an event bus.
/// </summary>
public interface IDomainEvent
{
    /// <summary>UTC timestamp at which the event occurred.</summary>
    DateTimeOffset OccurredOn { get; }
}
