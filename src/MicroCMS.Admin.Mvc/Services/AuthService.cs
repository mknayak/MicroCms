using MicroCMS.Admin.Mvc.Extensions;
using MicroCMS.Admin.Mvc.Infrastructure;
using MicroCMS.Admin.Mvc.Models.ApiDtos;
using MicroCMS.Admin.Mvc.Services.Abstractions;
using System.Net.Http.Json;
using System.Text.Json;

namespace MicroCMS.Admin.Mvc.Services;

/// <summary>
/// Calls the MicroCMS authentication API endpoints.
/// Login does NOT use the <see cref="TokenDelegatingHandler"/> because the
/// token has not yet been obtained at that point.
/// </summary>
public sealed class AuthService : IAuthService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly IHttpClientFactory _httpClientFactory;

    public AuthService(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<AuthTokenResponse> LoginAsync(string email, string password, CancellationToken ct = default)
    {
        using var client = _httpClientFactory.CreateClient(HttpClientNames.ApiClient);
        var body = new { email, password };
        var response = await client.PostAsJsonAsync("auth/login", body, ct);

        if (!response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync(ct);
            throw new ApiException((int)response.StatusCode, "Login failed.", content);
        }

        var result = await response.Content.ReadFromJsonAsync<AuthTokenResponse>(JsonOptions, ct);
        return result ?? throw new ApiException(500, "Empty login response.");
    }

    public async Task LogoutAsync(string refreshToken, CancellationToken ct = default)
    {
        using var client = _httpClientFactory.CreateClient(HttpClientNames.ApiClient);
        var body = new { refreshToken };
        await client.PostAsJsonAsync("auth/logout", body, ct);
        // Ignore non-success — we clear the local session regardless
    }

    public async Task<AuthTokenResponse> RefreshAsync(string refreshToken, CancellationToken ct = default)
    {
        using var client = _httpClientFactory.CreateClient(HttpClientNames.ApiClient);
        var body = new { refreshToken };
        var response = await client.PostAsJsonAsync("auth/refresh", body, ct);

        if (!response.IsSuccessStatusCode)
        {
            throw new ApiException((int)response.StatusCode, "Token refresh failed.");
        }

        var result = await response.Content.ReadFromJsonAsync<AuthTokenResponse>(JsonOptions, ct);
        return result ?? throw new ApiException(500, "Empty refresh response.");
    }

    public async Task ChangePasswordAsync(string currentPassword, string newPassword, CancellationToken ct = default)
    {
        using var client = _httpClientFactory.CreateClient(HttpClientNames.ApiClient);
        var body = new { currentPassword, newPassword };
        var response = await client.PostAsJsonAsync("auth/change-password", body, ct);

        if (!response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync(ct);
            throw new ApiException((int)response.StatusCode, "Password change failed.", content);
        }
    }
}
