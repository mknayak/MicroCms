using MediatR;

namespace MicroCMS.Domain.Events;

/// <summary>
/// Marker interface for all domain events.
/// Implements <see cref="INotification"/> so MediatR can dispatch them as notifications.
/// </summary>
public interface IDomainEvent : INotification
{
    /// <summary>UTC timestamp at which the event occurred.</summary>
    DateTimeOffset OccurredOn { get; }
}
