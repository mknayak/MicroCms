using MicroCMS.Admin.Mvc.Models.ApiDtos;

namespace MicroCMS.Admin.Mvc.Services.Abstractions;

/// <summary>
/// Provides authentication operations against the MicroCMS API.
/// </summary>
public interface IAuthService
{
    Task<AuthTokenResponse> LoginAsync(string email, string password, CancellationToken ct = default);
    Task LogoutAsync(string refreshToken, CancellationToken ct = default);
    Task<AuthTokenResponse> RefreshAsync(string refreshToken, CancellationToken ct = default);
    Task ChangePasswordAsync(string currentPassword, string newPassword, CancellationToken ct = default);
}
