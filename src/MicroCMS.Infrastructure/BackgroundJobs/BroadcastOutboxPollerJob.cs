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
/// Quartz job that processes <see cref="OutboxDispatchMode.Broadcast"/> outbox messages for
/// this host instance.
///
/// Unlike <see cref="OutboxDispatcherJob"/> (Exclusive — one instance claims the row),
/// Broadcast messages must be dispatched independently by every registered host instance
/// (CM, CD-01, CD-02, Preview, …) because each instance holds its own in-process state
/// (e.g. an <c>IMemoryCache</c>) that must be invalidated locally.
///
/// Per-instance progress is tracked in <see cref="OutboxDeliveryRecord"/>:
///   (MessageId, InstanceId) PK guarantees exactly-once delivery per instance.
///
/// The instance identity is configured via <c>MicroCMS:Outbox:InstanceId</c>
/// (e.g. "CM", "CD-01", "Preview"). Falls back to <c>Environment.MachineName</c>.
/// </summary>
[DisallowConcurrentExecution]
public sealed class BroadcastOutboxPollerJob : IJob
{
    private const int BatchSize = 100;
    private const int RetentionDays = 7;

    private static readonly JsonSerializerOptions _jsonOpts = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<BroadcastOutboxPollerJob> _logger;
    private readonly string _instanceId;

    public BroadcastOutboxPollerJob(
        IServiceScopeFactory scopeFactory,
        ILogger<BroadcastOutboxPollerJob> logger,
        OutboxInstanceOptions options)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _instanceId = options.InstanceId;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        var ct = context.CancellationToken;

        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var publisher = scope.ServiceProvider.GetRequiredService<IPublisher>();

        // Fetch Broadcast messages this instance has NOT yet delivered.
        // LEFT JOIN approach: OutboxMessages that have no OutboxDeliveryRecord for this instance.
        var cutoff = DateTimeOffset.UtcNow.AddDays(-RetentionDays);

        var pending = await db.Set<OutboxMessage>()
            .Where(m => m.DispatchMode == OutboxDispatchMode.Broadcast
                     && m.OccurredOnUtc >= cutoff)
            .Where(m => !db.Set<OutboxDeliveryRecord>()
                .Any(r => r.MessageId == m.Id && r.InstanceId == _instanceId))
            .OrderBy(m => m.OccurredOnUtc)
            .Take(BatchSize)
            .ToListAsync(ct);

        if (pending.Count == 0)
            return;

        _logger.LogDebug(
            "BroadcastOutboxPoller [{Instance}]: dispatching {Count} message(s).",
            _instanceId, pending.Count);

        foreach (var message in pending)
            await DispatchMessageAsync(message, db, publisher, ct);

        await db.SaveChangesAsync(CancellationToken.None);
    }

    private async Task DispatchMessageAsync(
        OutboxMessage message,
        ApplicationDbContext db,
        IPublisher publisher,
        CancellationToken ct)
    {
        try
        {
            var eventType = Type.GetType(message.Type);
            if (eventType is null)
            {
                _logger.LogWarning(
                    "BroadcastOutboxPoller [{Instance}]: unknown event type '{Type}' — skipping message {Id}.",
                    _instanceId, message.Type, message.Id);
                // Record as delivered so we don't retry an unresolvable type forever.
                db.Set<OutboxDeliveryRecord>().Add(
                    new OutboxDeliveryRecord(message.Id, _instanceId, DateTimeOffset.UtcNow));
                return;
            }

            var domainEvent = (IDomainEvent?)JsonSerializer.Deserialize(message.Content, eventType, _jsonOpts);
            if (domainEvent is null)
            {
                _logger.LogWarning(
                    "BroadcastOutboxPoller [{Instance}]: failed to deserialise message {Id} as {Type}.",
                    _instanceId, message.Id, message.Type);
                // Do not record delivery — allow retry on next poll.
                return;
            }

            var notificationType = typeof(DomainEventNotification<>).MakeGenericType(eventType);
            var notification = (INotification)Activator.CreateInstance(notificationType, domainEvent)!;

            await publisher.Publish(notification, ct);

            db.Set<OutboxDeliveryRecord>().Add(
                new OutboxDeliveryRecord(message.Id, _instanceId, DateTimeOffset.UtcNow));

            _logger.LogDebug(
                "BroadcastOutboxPoller [{Instance}]: dispatched {Type} (id={Id}).",
                _instanceId, eventType.Name, message.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex, "BroadcastOutboxPoller [{Instance}]: error dispatching message {Id} ({Type}).",
                _instanceId, message.Id, message.Type);
            // Do not record delivery — will retry on next poll interval.
        }
    }
}
