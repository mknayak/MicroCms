namespace MicroCMS.Domain.Events;

/// <summary>
/// Convenience base record for domain events providing a default <see cref="OccurredOn"/> timestamp.
/// </summary>
public abstract record DomainEvent : IDomainEvent
{
    public DateTimeOffset OccurredOn { get; } = DateTimeOffset.UtcNow;
}
