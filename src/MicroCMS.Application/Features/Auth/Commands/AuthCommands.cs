using MediatR;
using MicroCMS.Application.Features.Auth.Dtos;
using MicroCMS.Shared.Results;

namespace MicroCMS.Application.Features.Auth.Commands;

/// <summary>Authenticates a user with email + password and issues tokens.</summary>
public sealed record LoginCommand(
    string Email,
    string Password,
    string? IpAddress = null,
    string? UserAgent = null) : IRequest<Result<AuthTokenResponse>>;

/// <summary>Rotates a refresh token and issues a new access + refresh token pair.</summary>
public sealed record RefreshTokenCommand(
    string RefreshToken) : IRequest<Result<AuthTokenResponse>>;

/// <summary>Revokes the supplied refresh token (single-device logout).</summary>
public sealed record RevokeTokenCommand(
    string RefreshToken) : IRequest<Result>;

/// <summary>Revokes all active refresh tokens for the current user (all-devices logout).</summary>
public sealed record RevokeAllTokensCommand : IRequest<Result>;

/// <summary>Changes the authenticated user's password.</summary>
public sealed record ChangePasswordCommand(
    string CurrentPassword,
    string NewPassword) : IRequest<Result>;

/// <summary>Sets an initial password for a user who was invited (no existing credential).</summary>
public sealed record SetInitialPasswordCommand(
    Guid UserId,
    string NewPassword) : IRequest<Result>;
