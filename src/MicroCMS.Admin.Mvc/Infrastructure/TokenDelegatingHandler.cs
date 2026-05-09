using System.Net.Http.Headers;
using System.Security.Claims;

namespace MicroCMS.Admin.Mvc.Infrastructure;

/// <summary>
/// Outbound HTTP message handler that attaches the JWT access token from the
/// current user's cookie-based claims to every API request.
///
/// The token is stored as a claim of type <c>ClaimTypes.UserData</c> when the
/// user signs in via <see cref="Controllers.AccountController"/>.
/// </summary>
internal sealed class TokenDelegatingHandler : DelegatingHandler
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public TokenDelegatingHandler(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var token = GetAccessToken();

        if (!string.IsNullOrWhiteSpace(token))
        {
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", token);
        }

        return base.SendAsync(request, cancellationToken);
    }

    private string? GetAccessToken()
    {
        var user = _httpContextAccessor.HttpContext?.User;

        return user?.FindFirst(ClaimTypes.UserData)?.Value;
    }
}
