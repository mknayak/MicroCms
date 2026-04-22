using MicroCMS.Domain.Exceptions;
using MicroCMS.Shared.Ids;

namespace MicroCMS.Domain.Aggregates.Identity;

/// <summary>
/// Persisted refresh-token record.
/// The <see cref="TokenHash"/> is a SHA-256 hex-digest of the raw opaque token value.
/// The raw value is shown to the client exactly once and is never stored.
///
/// Rotation family (<see cref="FamilyId"/>) tracks token chains: if an already-used
/// token in a family is presented again, the entire family is immediately revoked
/// to neutralise token-theft replay attacks.
/// </summary>
public sealed class RefreshToken : AggregateRoot<RefreshTokenId>
{
    private RefreshToken() : base() { } // EF Core

    private RefreshToken(
        RefreshTokenId id,
        UserId userId,
        TenantId tenantId,
        string tokenHash,
        Guid familyId,
        DateTimeOffset expiresAt) : base(id)
    {
        UserId = userId;
        TenantId = tenantId;
        TokenHash = tokenHash;
        FamilyId = familyId;
        ExpiresAt = expiresAt;
        IsRevoked = false;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public UserId UserId { get; private set; }
    public TenantId TenantId { get; private set; }

    /// <summary>SHA-256 hex digest of the raw opaque token.</summary>
    public string TokenHash { get; private set; } = string.Empty;

    /// <summary>
    /// Rotation family GUID. All tokens generated from a single login share the same
    /// family. Presenting an already-consumed token triggers family-wide revocation.
    /// </summary>
    public Guid FamilyId { get; private set; }

    public DateTimeOffset ExpiresAt { get; private set; }
    public bool IsRevoked { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>
    /// Hash of the token that superseded this one; set when this token is rotated.
    /// Null means this token has not been consumed yet.
    /// </summary>
    public string? ReplacedByTokenHash { get; private set; }

    // ── Computed ──────────────────────────────────────────────────────────

    public bool IsExpired() => DateTimeOffset.UtcNow >= ExpiresAt;
    public bool IsActive() => !IsRevoked && !IsExpired();

    // ── Factory ────────────────────────────────────────────────────────────

    /// <summary>Creates the first token in a new rotation family (issued on login).</summary>
    public static RefreshToken CreateNew(
        UserId userId,
        TenantId tenantId,
        string tokenHash,
        DateTimeOffset expiresAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tokenHash, nameof(tokenHash));
        if (expiresAt <= DateTimeOffset.UtcNow)
            throw new DomainException("Refresh token expiry must be in the future.");

        return new RefreshToken(RefreshTokenId.New(), userId, tenantId, tokenHash, Guid.NewGuid(), expiresAt);
    }

    /// <summary>Creates a rotated successor token in the same family.</summary>
    public static RefreshToken CreateRotated(
        UserId userId,
        TenantId tenantId,
        string tokenHash,
        Guid familyId,
        DateTimeOffset expiresAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tokenHash, nameof(tokenHash));
        return new RefreshToken(RefreshTokenId.New(), userId, tenantId, tokenHash, familyId, expiresAt);
    }

    // ── State mutations ───────────────────────────────────────────────────

    /// <summary>Marks this token as consumed and records the hash of its replacement.</summary>
    public void Consume(string replacedByTokenHash)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(replacedByTokenHash, nameof(replacedByTokenHash));
        if (IsRevoked)
            throw new BusinessRuleViolationException("RefreshToken.AlreadyRevoked", "Token has already been revoked.");
        if (IsExpired())
            throw new BusinessRuleViolationException("RefreshToken.Expired", "Token has expired.");

        IsRevoked = true;
        ReplacedByTokenHash = replacedByTokenHash;
    }

    /// <summary>Revokes this token immediately (e.g., on logout or replay detection).</summary>
    public void Revoke()
    {
        IsRevoked = true;
    }
}
