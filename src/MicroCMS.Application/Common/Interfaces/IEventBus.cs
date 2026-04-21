using MicroCMS.Domain.Events;

namespace MicroCMS.Application.Common.Interfaces;

/// <summary>
/// In-process event bus. The outbox dispatcher calls this after the transaction commits.
/// </summary>
public interface IEventBus
{
    Task PublishAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default);
}
