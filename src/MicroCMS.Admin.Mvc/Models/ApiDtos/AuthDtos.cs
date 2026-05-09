namespace MicroCMS.Admin.Mvc.Models.ApiDtos;

public sealed class LoginRequest
{
    public string Email { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
}

public sealed class AuthTokenResponse
{
    public string AccessToken { get; init; } = string.Empty;
    public string RefreshToken { get; init; } = string.Empty;
    public string AccessTokenExpiry { get; init; } = string.Empty;
    public string RefreshTokenExpiry { get; init; } = string.Empty;
    public AuthUserDto User { get; init; } = new();
}

public sealed class AuthUserDto
{
    public string UserId { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public List<string> Roles { get; init; } = [];
}

public sealed class ChangePasswordRequest
{
    public string CurrentPassword { get; init; } = string.Empty;
    public string NewPassword { get; init; } = string.Empty;
}

public sealed class RefreshTokenRequest
{
    public string RefreshToken { get; init; } = string.Empty;
}
