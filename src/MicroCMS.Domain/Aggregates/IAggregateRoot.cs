using MicroCMS.Domain.Events;

namespace MicroCMS.Domain.Aggregates;

/// <summary>
/// Marker interface for all aggregate roots to enable non-generic operations like event collection.
/// </summary>
public interface IAggregateRoot
{
 IReadOnlyList<IDomainEvent> DomainEvents { get; }
    void ClearDomainEvents();
}
