using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using MicroCMS.Domain.Aggregates.Tenant;
using MicroCMS.Domain.Repositories;
using MicroCMS.Domain.Specifications.Tenant;
using MicroCMS.Shared.Ids;
using Microsoft.Extensions.DependencyInjection;

namespace MicroCMS.Api.Middleware;

/// <summary>
/// Validates the <c>X-Api-Key</c> request header and, on success, synthesises a
/// <see cref="ClaimsPrincipal"/> so that downstream authorization middleware treats
/// the request as authenticated via the <c>api_key</c> auth method.
///
/// API keys are stored as SHA-256 hex hashes; the raw key is never persisted.
/// The middleware short-circuits on bearer-authenticated requests so it never
/// interferes with normal JWT flows.
/// </summary>
public sealed class ApiKeyAuthenticationMiddleware(RequestDelegate next)
{
    private const string ApiKeyHeader = "X-Api-Key";

    public async Task InvokeAsync(HttpContext context)
    {
        // Skip API key check if a Bearer token is already present
        if (context.Request.Headers.ContainsKey("Authorization"))
        {
            await next(context);
            return;
        }

        if (!context.Request.Headers.TryGetValue(ApiKeyHeader, out var rawKey) ||
            string.IsNullOrWhiteSpace(rawKey))
        {
            await next(context);
            return;
        }

        var hash = HashKey(rawKey.ToString());

        // Resolve scoped repository from request services (not from constructor)
        var repo = context.RequestServices.GetRequiredService<IRepository<ApiClient, ApiClientId>>();
        var clients = await repo.ListAsync(new ApiClientByHashSpec(hash), context.RequestAborted);
        var client = clients.FirstOrDefault(c => c.IsActive && (c.ExpiresAt is null || c.ExpiresAt > DateTimeOffset.UtcNow));

        if (client is null)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new { error = "Invalid or expired API key." });
            return;
        }

        // Synthesise principal with minimal claims
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, client.Id.Value.ToString()),
            new("tenant_id", client.TenantId.Value.ToString()),
            new("site_id", client.SiteId.Value.ToString()),
            new("auth_method", "api_key"),
            new("api_key_type", client.KeyType.ToString()),
        };

        foreach (var scope in client.Scopes)
        {
            claims.Add(new Claim("scope", scope));
        }

        var identity = new ClaimsIdentity(claims, authenticationType: "ApiKey");
        context.User = new ClaimsPrincipal(identity);

        await next(context);
    }

    private static string HashKey(string rawKey)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawKey));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
