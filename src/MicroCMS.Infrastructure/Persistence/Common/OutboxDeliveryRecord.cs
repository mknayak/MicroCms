using MicroCMS.Domain.Events;

namespace MicroCMS.Infrastructure.Persistence.Common;

/// <summary>
/// Tracks per-instance delivery of <see cref="OutboxDispatchMode.Broadcast"/> outbox messages.
///
/// One row is written by each host instance after it successfully dispatches a Broadcast message.
/// The <see cref="InstanceId"/> is a stable identifier for the host process, set at startup
/// (e.g. "CM", "CD-01", "CD-02", "Preview").
///
/// This allows the <c>BroadcastOutboxPollerJob</c> to query:
///   "Give me Broadcast messages that I (<see cref="InstanceId"/>) have NOT yet delivered."
/// without interfering with other instances or with the Exclusive dispatcher.
/// </summary>
public sealed class OutboxDeliveryRecord
{
    private OutboxDeliveryRecord() { } // EF Core

    public OutboxDeliveryRecord(
        Guid messageId,
        string instanceId,
        DateTimeOffset processedOnUtc)
    {
        MessageId = messageId;
        InstanceId = instanceId;
        ProcessedOnUtc = processedOnUtc;
    }

    /// <summary>FK to <see cref="OutboxMessage.Id"/>.</summary>
    public Guid MessageId { get; private set; }

    /// <summary>
    /// Stable identifier of the host instance that processed this message.
    /// Configured via <c>MicroCMS:Outbox:InstanceId</c> (e.g. "CM", "CD-01", "Preview").
    /// Falls back to the machine name when not configured.
    /// </summary>
    public string InstanceId { get; private set; } = string.Empty;

    /// <summary>UTC time at which this instance successfully dispatched the message.</summary>
    public DateTimeOffset ProcessedOnUtc { get; private set; }
}
