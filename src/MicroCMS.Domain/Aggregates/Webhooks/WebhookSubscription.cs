using MicroCMS.Domain.Exceptions;
using MicroCMS.Shared.Ids;

namespace MicroCMS.Domain.Aggregates.Webhooks;

/// <summary>Strongly-typed ID for <c>WebhookSubscription</c>.</summary>
public readonly record struct WebhookSubscriptionId(Guid Value)
{
    public static WebhookSubscriptionId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString();
}

/// <summary>
/// Webhook subscription aggregate (GAP-19).
/// Stores target URL, event filter list, HMAC secret (hashed), retry policy,
/// and recent delivery log entries (capped at 50 for UI display).
/// </summary>
public sealed class WebhookSubscription : AggregateRoot<WebhookSubscriptionId>
{
    public const int MaxUrlLength = 500;
    public const int MaxDeliveryLogEntries = 50;

  private readonly List<string> _events = [];
    private readonly List<WebhookDeliveryLog> _deliveryLogs = [];

    private WebhookSubscription() : base() { } // EF Core

    private WebhookSubscription(
        WebhookSubscriptionId id,
  TenantId tenantId,
  SiteId? siteId,
        string targetUrl,
        string hashedSecret) : base(id)
    {
    TenantId = tenantId;
        SiteId = siteId;
        TargetUrl = targetUrl;
     HashedSecret = hashedSecret;
      IsActive = true;
     CreatedAt = DateTimeOffset.UtcNow;
    }

    public TenantId TenantId { get; private set; }
    /// <summary>Null means this is a tenant-level subscription (applies to all sites).</summary>
    public SiteId? SiteId { get; private set; }
    public string TargetUrl { get; private set; } = string.Empty;
    /// <summary>bcrypt hash of the webhook signing secret. Raw value shown once on creation.</summary>
    public string HashedSecret { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }
    public int MaxRetries { get; private set; } = 3;
    public DateTimeOffset CreatedAt { get; private set; }
    public IReadOnlyList<string> Events => _events.AsReadOnly();
    public IReadOnlyList<WebhookDeliveryLog> DeliveryLogs => _deliveryLogs.AsReadOnly();

    // ── Factory ────────────────────────────────────────────────────────────

    public static WebhookSubscription Create(
        TenantId tenantId,
     SiteId? siteId,
    string targetUrl,
  string hashedSecret,
        IEnumerable<string> events,
        int maxRetries = 3)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(targetUrl, nameof(targetUrl));
      ArgumentException.ThrowIfNullOrWhiteSpace(hashedSecret, nameof(hashedSecret));

        if (!Uri.TryCreate(targetUrl, UriKind.Absolute, out _))
            throw new DomainException($"Webhook target URL '{targetUrl}' must be a valid absolute URL.");

        if (targetUrl.Length > MaxUrlLength)
  throw new DomainException($"Webhook target URL must not exceed {MaxUrlLength} characters.");

  var sub = new WebhookSubscription(WebhookSubscriptionId.New(), tenantId, siteId, targetUrl, hashedSecret);
        sub._events.AddRange(events.Select(e => e.Trim()).Where(e => e.Length > 0).Distinct());
      sub.MaxRetries = Math.Clamp(maxRetries, 0, 10);
        return sub;
    }

    // ── Mutations ─────────────────────────────────────────────────────────

    public void UpdateEvents(IEnumerable<string> events)
    {
 _events.Clear();
    _events.AddRange(events.Select(e => e.Trim()).Where(e => e.Length > 0).Distinct());
    }

 public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;

  public void RecordDelivery(string eventType, int statusCode, string? errorMessage = null)
    {
     _deliveryLogs.Insert(0, new WebhookDeliveryLog(eventType, statusCode, errorMessage, DateTimeOffset.UtcNow));
     if (_deliveryLogs.Count > MaxDeliveryLogEntries)
      _deliveryLogs.RemoveAt(_deliveryLogs.Count - 1);
    }
}

/// <summary>Immutable log entry for a single webhook dispatch attempt.</summary>
public sealed record WebhookDeliveryLog(
    string EventType,
    int StatusCode,
    string? ErrorMessage,
    DateTimeOffset DeliveredAt)
{
    public bool IsSuccess => StatusCode is >= 200 and < 300;
}
