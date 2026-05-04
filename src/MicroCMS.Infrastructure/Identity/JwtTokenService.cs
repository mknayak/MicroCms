using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using MicroCMS.Application.Common.Interfaces;
using MicroCMS.Domain.Aggregates.Identity;
using MicroCMS.Shared.Ids;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace MicroCMS.Infrastructure.Identity;

/// <summary>
/// Issues and validates JWT access tokens, and generates cryptographically-secure
/// opaque refresh tokens. Refresh tokens are stored as SHA-256 hashes only.
/// </summary>
internal sealed class JwtTokenService : ITokenService
{
    private const int RefreshTokenByteLength = 64; // 512 bits → 88-char base64url

    private readonly string _secret;
    private readonly string _issuer;
    private readonly string _audience;
    private readonly int _accessTokenMinutes;

    public JwtTokenService(IConfiguration configuration)
    {
        var jwtSection = configuration.GetSection("Jwt");
        _secret = jwtSection["Secret"] ?? throw new InvalidOperationException("Jwt:Secret is not configured.");
        _issuer = jwtSection["Issuer"] ?? "microcms";
        _audience = jwtSection["Audience"] ?? "microcms-api";
        _accessTokenMinutes = jwtSection.GetValue<int>("AccessTokenMinutes", defaultValue: 15);

        if (_secret.Length < 32)
            throw new InvalidOperationException("Jwt:Secret must be at least 32 characters.");
    }

    /// <inheritdoc/>
    public string GenerateAccessToken(User user, SiteId? siteId = null)
    {
        ArgumentNullException.ThrowIfNull(user);

        var claims = BuildClaims(user, siteId);
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _issuer,
            audience: _audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: DateTime.UtcNow.AddMinutes(_accessTokenMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <inheritdoc/>
    public (string RawToken, string TokenHash) GenerateRefreshToken()
    {
        Span<byte> bytes = stackalloc byte[RefreshTokenByteLength];
        RandomNumberGenerator.Fill(bytes);
        var raw = Convert.ToBase64String(bytes);
        return (raw, HashToken(raw));
    }

    /// <inheritdoc/>
    public string HashToken(string rawToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rawToken, nameof(rawToken));
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawToken));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    // ── Private helpers ───────────────────────────────────────────────────

    private static IEnumerable<Claim> BuildClaims(User user, SiteId? siteId)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.Value.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email.Value),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new("tenant_id", user.TenantId.Value.ToString()),
            new("display_name", user.DisplayName.Value),
        };

        if (siteId is not null)
            claims.Add(new Claim("site_id", siteId.Value.ToString()));

        foreach (var role in user.Roles)
        {
            claims.Add(new Claim("role", role.Name));
            if (role.SiteId.HasValue)
                claims.Add(new Claim($"site_role:{role.SiteId.Value}", role.Name));
        }

        return claims;
    }
}
