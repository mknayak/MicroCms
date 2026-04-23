using System.Security.Cryptography;
using System.Text;
using MicroCMS.Application.Common.Interfaces;
using MicroCMS.Domain.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;

namespace MicroCMS.Infrastructure.Storage.Signing;

/// <summary>
/// Generates time-limited HMAC-SHA256 signed URLs for media assets served through the API.
/// The HMAC payload includes the storage key, expiry timestamp, and the tenant ID so that
/// a signed URL issued to one tenant cannot be used by another.
/// </summary>
public sealed class HmacStorageSigningService : IStorageSigningService
{
    private readonly HmacSigningOptions _options;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly LinkGenerator _linkGenerator;
    private readonly ICurrentUser _currentUser;

    public HmacStorageSigningService(
        IOptions<HmacSigningOptions> options,
        IHttpContextAccessor httpContextAccessor,
        LinkGenerator linkGenerator,
        ICurrentUser currentUser)
    {
        _options = options.Value;
        _httpContextAccessor = httpContextAccessor;
        _linkGenerator = linkGenerator;
        _currentUser = currentUser;
    }

    public Task<string> GenerateSignedUrlAsync(
        string storageKey,
        TimeSpan expiresIn,
        CancellationToken cancellationToken = default)
    {
        var expiresAt = DateTimeOffset.UtcNow.Add(expiresIn).ToUnixTimeSeconds();
        var tenantId = _currentUser.TenantId.Value.ToString();

        var signature = ComputeSignature(storageKey, expiresAt, tenantId);

        var baseUrl = GetBaseUrl();
        var url = $"{baseUrl}/api/v1/media/serve?" +
                  $"key={Uri.EscapeDataString(storageKey)}" +
                  $"&exp={expiresAt}" +
                  $"&tid={Uri.EscapeDataString(tenantId)}" +
                  $"&sig={Uri.EscapeDataString(signature)}";

        return Task.FromResult(url);
    }

    /// <summary>
    /// Validates a signed URL's HMAC and expiry. Returns true when the URL is valid.
    /// </summary>
    public bool Validate(string storageKey, long expiresAt, string tenantId, string signature)
    {
        if (DateTimeOffset.UtcNow.ToUnixTimeSeconds() > expiresAt)
            return false;

        var expected = ComputeSignature(storageKey, expiresAt, tenantId);
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(expected),
            Encoding.UTF8.GetBytes(signature));
    }

    // ── Private helpers ───────────────────────────────────────────────────

    private string ComputeSignature(string storageKey, long expiresAt, string tenantId)
    {
        var payload = $"{storageKey}:{expiresAt}:{tenantId}";
        var keyBytes = Encoding.UTF8.GetBytes(_options.SigningKey);
        var payloadBytes = Encoding.UTF8.GetBytes(payload);

        var hash = HMACSHA256.HashData(keyBytes, payloadBytes);
        return Convert.ToBase64String(hash);
    }

    private string GetBaseUrl()
    {
        var ctx = _httpContextAccessor.HttpContext;
        if (ctx is null)
            return _options.BaseUrl ?? string.Empty;

        return $"{ctx.Request.Scheme}://{ctx.Request.Host}";
    }
}
