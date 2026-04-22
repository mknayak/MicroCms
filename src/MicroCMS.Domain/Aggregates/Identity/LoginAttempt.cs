using MicroCMS.Shared.Ids;

namespace MicroCMS.Domain.Aggregates.Identity;

/// <summary>
/// Immutable audit record of a login attempt (successful or failed).
/// Used by <c>ILoginAttemptService</c> to enforce brute-force lockout policy
/// and to feed the security audit trail.
/// </summary>
public sealed class LoginAttempt : AggregateRoot<LoginAttemptId>
{
    private LoginAttempt() : base() { } // EF Core

    private LoginAttempt(
        LoginAttemptId id,
        TenantId tenantId,
        string email,
        bool isSuccessful,
        string? ipAddress,
        string? userAgent) : base(id)
    {
        TenantId = tenantId;
        Email = email;
        IsSuccessful = isSuccessful;
        IpAddress = ipAddress;
        UserAgent = userAgent;
        AttemptedAt = DateTimeOffset.UtcNow;
    }

    public TenantId TenantId { get; private set; }
    public string Email { get; private set; } = string.Empty;
    public bool IsSuccessful { get; private set; }

    /// <summary>Raw IP address string (IPv4 or IPv6). Null when not resolvable.</summary>
    public string? IpAddress { get; private set; }
    public string? UserAgent { get; private set; }
    public DateTimeOffset AttemptedAt { get; private set; }

    // ── Factory ────────────────────────────────────────────────────────────

    public static LoginAttempt Record(
        TenantId tenantId,
        string email,
        bool isSuccessful,
        string? ipAddress = null,
        string? userAgent = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email, nameof(email));
        return new LoginAttempt(LoginAttemptId.New(), tenantId, email.ToLowerInvariant(), isSuccessful, ipAddress, userAgent);
    }
}
