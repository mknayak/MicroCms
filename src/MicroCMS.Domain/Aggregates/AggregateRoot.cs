using MicroCMS.Domain.Events;

namespace MicroCMS.Domain.Aggregates;

/// <summary>
/// Base class for all DDD aggregate roots.
/// Collects domain events that are dispatched after the transaction commits.
/// </summary>
public abstract class AggregateRoot
{
    private readonly List<IDomainEvent> _domainEvents = [];

    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected void RaiseDomainEvent(IDomainEvent domainEvent) =>
        _domainEvents.Add(domainEvent);

    public void ClearDomainEvents() =>
        _domainEvents.Clear();
}
