using MicroCMS.Domain.Aggregates.Identity;

namespace MicroCMS.Application.Common.Interfaces;

/// <summary>
/// Generates and validates JWT access tokens and opaque refresh tokens.
/// ADR-007: token strategy — short-lived JWT (15 min) + long-lived refresh token (7 days)
/// with per-use rotation and family-based replay detection.
/// </summary>
public interface ITokenService
{
    /// <summary>
    /// Issues a signed JWT access token for the given user.
    /// Token lifetime is controlled by <c>Jwt:AccessTokenMinutes</c> configuration.
    /// </summary>
    string GenerateAccessToken(User user);

    /// <summary>
    /// Generates a cryptographically-secure opaque refresh token.
    /// Returns both the raw value (to be sent to the client) and its SHA-256 hash
    /// (to be persisted). The raw value is never stored.
    /// </summary>
    (string RawToken, string TokenHash) GenerateRefreshToken();

    /// <summary>
    /// Computes the SHA-256 hex digest of a raw token value, for persistence and lookup.
    /// </summary>
    string HashToken(string rawToken);
}
