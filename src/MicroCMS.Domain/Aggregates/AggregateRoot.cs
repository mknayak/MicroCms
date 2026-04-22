using MicroCMS.Domain.Entities;
using MicroCMS.Domain.Events;

namespace MicroCMS.Domain.Aggregates;

/// <summary>
/// Base class for all DDD aggregate roots.
/// Collects domain events that are dispatched after the transaction commits.
/// </summary>
public abstract class AggregateRoot<TId> : Entity<TId>, IAggregateRoot
    where TId : notnull
{
    private readonly List<IDomainEvent> _domainEvents = [];

    protected AggregateRoot(TId id) : base(id) { }

    // EF Core
    protected AggregateRoot() : base() { }

    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected void RaiseDomainEvent(IDomainEvent domainEvent) =>
      _domainEvents.Add(domainEvent);

    public void ClearDomainEvents() =>
        _domainEvents.Clear();
}
