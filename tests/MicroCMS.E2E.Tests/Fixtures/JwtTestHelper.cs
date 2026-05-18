using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace MicroCMS.E2E.Tests.Fixtures;

/// <summary>
/// Minimal JWT generator for E2E test authentication.
/// Uses the same symmetric HS256 algorithm as <c>JwtTokenService</c> in Infrastructure.
/// </summary>
internal static class JwtTestHelper
{
    public static string GenerateToken(
        string secret,
        string issuer,
        string audience,
        string tenantId,
        string role,
        int expiryMinutes = 60)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, Guid.NewGuid().ToString()),
            new Claim("tenant_id", tenantId),
            new Claim("role", role),
        };

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expiryMinutes),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
