using MicroCMS.Application.Features.Auth.Commands;
using MicroCMS.Application.Features.Auth.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MicroCMS.Api.Controllers;

/// <summary>
/// Handles authentication: login, token refresh, logout (single + all devices),
/// and password management.
///
/// All endpoints except <see cref="Login"/> and <see cref="RefreshToken"/>
/// require a valid JWT bearer token.
/// </summary>
public sealed class AuthController : ApiControllerBase
{
    // ── POST /api/v1/auth/login ───────────────────────────────────────────

    /// <summary>Authenticates with email + password and returns token pair.</summary>
    [AllowAnonymous]
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthTokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = HttpContext.Request.Headers.UserAgent.ToString();

        var command = new LoginCommand(request.Email, request.Password, ipAddress, userAgent);
        var result = await Sender.Send(command, cancellationToken);
        return OkOrProblem(result);
    }

    // ── POST /api/v1/auth/refresh ─────────────────────────────────────────

    /// <summary>Rotates a refresh token and returns a new access + refresh token pair.</summary>
    [AllowAnonymous]
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(AuthTokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RefreshToken(
        [FromBody] RefreshTokenRequest request,
        CancellationToken cancellationToken)
    {
        var command = new RefreshTokenCommand(request.RefreshToken);
        var result = await Sender.Send(command, cancellationToken);
        return OkOrProblem(result);
    }

    // ── POST /api/v1/auth/logout ──────────────────────────────────────────

    /// <summary>Revokes the supplied refresh token (single-device logout).</summary>
    [Authorize]
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout(
        [FromBody] RevokeTokenRequest request,
        CancellationToken cancellationToken)
    {
        var command = new RevokeTokenCommand(request.RefreshToken);
        var result = await Sender.Send(command, cancellationToken);
        return NoContentOrProblem(result);
    }

    // ── POST /api/v1/auth/logout-all ─────────────────────────────────────

    /// <summary>Revokes all active sessions for the authenticated user.</summary>
    [Authorize]
    [HttpPost("logout-all")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> LogoutAll(CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new RevokeAllTokensCommand(), cancellationToken);
        return NoContentOrProblem(result);
    }

    // ── POST /api/v1/auth/change-password ────────────────────────────────

    /// <summary>Changes the authenticated user's password. Revokes all existing sessions.</summary>
    [Authorize]
    [HttpPost("change-password")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> ChangePassword(
        [FromBody] ChangePasswordRequest request,
        CancellationToken cancellationToken)
    {
        var command = new ChangePasswordCommand(request.CurrentPassword, request.NewPassword);
        var result = await Sender.Send(command, cancellationToken);
        return NoContentOrProblem(result);
    }

    // ── POST /api/v1/auth/set-password ───────────────────────────────────

    /// <summary>
    /// Sets the initial password for a user created via invitation.
    /// Requires TenantAdmin role or must be called by the target user
    /// (invite-token flow — Sprint 8).
    /// </summary>
    [Authorize]
    [HttpPost("set-password")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> SetInitialPassword(
        [FromBody] SetInitialPasswordRequest request,
        CancellationToken cancellationToken)
    {
        var command = new SetInitialPasswordCommand(request.UserId, request.NewPassword);
        var result = await Sender.Send(command, cancellationToken);
        return NoContentOrProblem(result);
    }
}

// ── Request models ─────────────────────────────────────────────────────────────

/// <summary>Login request body.</summary>
public sealed record LoginRequest(string Email, string Password);

/// <summary>Token refresh request body.</summary>
public sealed record RefreshTokenRequest(string RefreshToken);

/// <summary>Single-device logout request body.</summary>
public sealed record RevokeTokenRequest(string RefreshToken);

/// <summary>Change-password request body.</summary>
public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);

/// <summary>Set-initial-password request body.</summary>
public sealed record SetInitialPasswordRequest(Guid UserId, string NewPassword);
