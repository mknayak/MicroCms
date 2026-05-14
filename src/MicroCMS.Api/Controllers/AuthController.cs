using MicroCMS.Application.Features.Auth.Commands;
using MicroCMS.Application.Features.Auth.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;

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
        if (result.IsSuccess) AppendSiteCookie(result.Value.AccessToken);
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
        if (result.IsSuccess) AppendSiteCookie(result.Value.AccessToken);
        return OkOrProblem(result);
    }

    // ── POST /api/v1/auth/switch-site ─────────────────────────────────────

    /// <summary>
    /// Issues a new token pair scoped to the requested site.
    /// The <c>site_id</c> claim in the returned access token changes to the new site;
    /// all Category-1 APIs (entries, media, content types, etc.) will operate against it.
    /// The user must have at least one role on the target site.
    /// </summary>
    [Authorize]
    [HttpPost("switch-site")]
    [ProducesResponseType(typeof(AuthTokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> SwitchSite(
        [FromBody] SwitchSiteRequest request,
        CancellationToken cancellationToken)
    {
        var command = new SwitchSiteCommand(request.SiteId);
        var result = await Sender.Send(command, cancellationToken);
        if (result.IsSuccess) AppendSiteCookieDirect(request.SiteId);
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
        if (result.IsSuccess) ClearSiteCookie();
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
    // ── Cookie helpers ────────────────────────────────────────────────────

    // Cookie name the asset delivery endpoint reads to resolve SiteId.
    private const string SiteCookieName = "mcms_site";

    /// <summary>
    /// Decodes the JWT access token, reads the <c>site_id</c> claim, and appends
    /// an <c>HttpOnly; SameSite=Lax; Path=/</c> cookie so the browser sends it on
    /// every subsequent request — including anonymous asset delivery requests.
    /// </summary>
    private void AppendSiteCookie(string accessToken)
    {
        var handler = new JwtSecurityTokenHandler();
        if (!handler.CanReadToken(accessToken)) return;

        var jwt = handler.ReadJwtToken(accessToken);
        var siteIdClaim = jwt.Claims.FirstOrDefault(c => c.Type == "site_id")?.Value;
        if (string.IsNullOrEmpty(siteIdClaim)) return;

        AppendSiteCookieDirect(Guid.TryParse(siteIdClaim, out var g) ? g : Guid.Empty);
    }

    /// <summary>Writes the <c>mcms_site</c> cookie with the given <paramref name="siteId"/>.</summary>
    private void AppendSiteCookieDirect(Guid siteId)
    {
        if (siteId == Guid.Empty) return;

        Response.Cookies.Append(SiteCookieName, siteId.ToString(), new CookieOptions
        {
            HttpOnly  = true,
            SameSite  = SameSiteMode.Lax,
            Path      = "/",
            // No Expires → session cookie; cleared when browser closes.
            // The JWT itself enforces the real expiry; this cookie just carries the id.
        });
    }

    /// <summary>Clears the <c>mcms_site</c> cookie on logout.</summary>
    private void ClearSiteCookie() =>
        Response.Cookies.Delete(SiteCookieName, new CookieOptions { Path = "/" });
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
