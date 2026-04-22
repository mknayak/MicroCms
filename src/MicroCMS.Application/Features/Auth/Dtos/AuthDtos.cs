namespace MicroCMS.Application.Features.Auth.Dtos;

/// <summary>Returned to the client after a successful login or token refresh.</summary>
public sealed record AuthTokenResponse(
    string AccessToken,
    string RefreshToken,
    DateTimeOffset AccessTokenExpiry,
    DateTimeOffset RefreshTokenExpiry,
    AuthUserDto User);

/// <summary>Minimal user information embedded in the auth response.</summary>
public sealed record AuthUserDto(
    Guid UserId,
    string Email,
    string DisplayName,
    IReadOnlyList<string> Roles);
