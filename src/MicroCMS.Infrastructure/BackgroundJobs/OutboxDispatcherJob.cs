using System.Text.Json;
using MediatR;
using MicroCMS.Application.Common.Events;
using MicroCMS.Domain.Events;
using MicroCMS.Infrastructure.Persistence.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Quartz;

namespace MicroCMS.Infrastructure.BackgroundJobs;

/// <summary>
/// Quartz job that polls the outbox table for unprocessed <see cref="OutboxMessage"/> rows,
/// deserialises each domain event, wraps it in a <see cref="DomainEventNotification{T}"/>,
/// and publishes it via MediatR — delivering it to all registered <c>INotificationHandler</c>
/// implementations (search indexer, cache invalidation, etc.).
///
/// Uses <c>[DisallowConcurrentExecution]</c> to prevent overlapping runs.
/// Marks each message as processed atomically; failures are recorded so the message
/// can be retried on the next interval (up to <see cref="MaxRetries"/>).
/// </summary>
[DisallowConcurrentExecution]
public sealed class OutboxDispatcherJob : IJob
{
    private const int BatchSize = 50;
    private const int MaxRetries = 5;

    private static readonly JsonSerializerOptions _jsonOpts = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OutboxDispatcherJob> _logger;

    public OutboxDispatcherJob(IServiceScopeFactory scopeFactory, ILogger<OutboxDispatcherJob> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        var ct = context.CancellationToken;
        await using var scope = _scopeFactory.CreateAsyncScope();

        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var publisher = scope.ServiceProvider.GetRequiredService<IPublisher>();

        var messages = await db.Set<OutboxMessage>()
            .Where(m => m.ProcessedOnUtc == null && m.RetryCount < MaxRetries)
            .OrderBy(m => m.OccurredOnUtc)
            .Take(BatchSize)
            .ToListAsync(ct);

        if (messages.Count == 0)
            return;

        _logger.LogDebug("OutboxDispatcherJob: processing {Count} message(s).", messages.Count);

        foreach (var message in messages)
        {
            await DispatchMessageAsync(message, publisher, ct);
        }

        await db.SaveChangesAsync(CancellationToken.None);
    }

    private async Task DispatchMessageAsync(
        OutboxMessage message,
        IPublisher publisher,
        CancellationToken ct)
    {
        try
        {
            var eventType = Type.GetType(message.Type);
            if (eventType is null)
            {
                _logger.LogWarning(
                    "OutboxDispatcherJob: unknown event type '{Type}' — skipping message {Id}.",
                    message.Type, message.Id);
                message.MarkProcessed(DateTimeOffset.UtcNow);
                return;
            }

            var domainEvent = (IDomainEvent?)JsonSerializer.Deserialize(message.Content, eventType, _jsonOpts);
            if (domainEvent is null)
            {
                _logger.LogWarning(
                    "OutboxDispatcherJob: failed to deserialise message {Id} as {Type}.",
                    message.Id, message.Type);
                message.RecordFailure("Deserialisation returned null.");
                return;
            }

            var notificationType = typeof(DomainEventNotification<>).MakeGenericType(eventType);
            var notification = (INotification)Activator.CreateInstance(notificationType, domainEvent)!;

            await publisher.Publish(notification, ct);
            message.MarkProcessed(DateTimeOffset.UtcNow);

            _logger.LogDebug(
                "OutboxDispatcherJob: dispatched {Type} (id={Id}).", eventType.Name, message.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex, "OutboxDispatcherJob: error dispatching message {Id} ({Type}).",
                message.Id, message.Type);
            message.RecordFailure(ex.Message);
        }
    }
}
