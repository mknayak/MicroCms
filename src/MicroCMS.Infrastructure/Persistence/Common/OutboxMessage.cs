namespace MicroCMS.Infrastructure.Persistence.Common;

/// <summary>
/// Persistence entity for the transactional outbox pattern.
/// Domain events are serialised here within the same DB transaction as aggregate changes,
/// then asynchronously dispatched by the <c>OutboxDispatcher</c> background job (Sprint 10).
/// </summary>
public sealed class OutboxMessage
{
    private OutboxMessage() { } // EF Core

    public OutboxMessage(
        Guid id,
        string type,
        string content,
        Guid? tenantId,
        DateTimeOffset occurredOnUtc)
    {
        Id = id;
        Type = type;
        Content = content;
        TenantId = tenantId;
        OccurredOnUtc = occurredOnUtc;
    }

    /// <summary>Primary key — same Guid used in the domain event, enabling idempotency checks.</summary>
    public Guid Id { get; private set; }

    /// <summary>Assembly-qualified type name of the domain event (used for deserialization).</summary>
    public string Type { get; private set; } = string.Empty;

    /// <summary>JSON-serialised event payload.</summary>
    public string Content { get; private set; } = string.Empty;

    /// <summary>
    /// The tenant the event belongs to. Null for system-level events (e.g. tenant creation itself).
    /// Stored as raw Guid to avoid coupling the outbox table to the TenantId strong type.
    /// </summary>
    public Guid? TenantId { get; private set; }

    /// <summary>Wall-clock time (UTC) the domain event was raised.</summary>
    public DateTimeOffset OccurredOnUtc { get; private set; }

    /// <summary>Set by the dispatcher once the message is successfully published.</summary>
    public DateTimeOffset? ProcessedOnUtc { get; private set; }

    /// <summary>Last dispatcher error, if any. Cleared on successful delivery.</summary>
    public string? Error { get; private set; }

    /// <summary>Number of dispatch attempts (for exponential back-off).</summary>
    public int RetryCount { get; private set; }

    // ── State transitions ────────────────────────────────────────────────

    public void MarkProcessed(DateTimeOffset processedAt)
    {
        ProcessedOnUtc = processedAt;
        Error = null;
    }

    public void RecordFailure(string error)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(error, nameof(error));
        Error = error[..Math.Min(error.Length, 2000)]; // cap at 2 KB
        RetryCount++;
    }
}
