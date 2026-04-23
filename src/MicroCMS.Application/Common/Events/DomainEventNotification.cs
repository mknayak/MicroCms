using MediatR;
using MicroCMS.Domain.Events;

namespace MicroCMS.Application.Common.Events;

/// <summary>
/// Generic MediatR notification wrapper around a domain event.
/// Keeps the Domain layer free of MediatR dependencies while still allowing
/// application handlers to be notified via the MediatR <see cref="IPublisher"/> pipeline.
/// </summary>
public sealed record DomainEventNotification<TDomainEvent>(TDomainEvent DomainEvent) : INotification
    where TDomainEvent : IDomainEvent;
