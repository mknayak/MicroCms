using MicroCMS.Admin.Mvc.Infrastructure;
using MicroCMS.Admin.Mvc.Models.ViewModels.Auth;
using MicroCMS.Admin.Mvc.Services.Abstractions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace MicroCMS.Admin.Mvc.Controllers;

/// <summary>
/// Handles authentication: login, logout, and password management.
/// Login exchanges credentials for a JWT from the API, then issues a
/// cookie-based session so the user never touches JWT tokens directly.
/// </summary>
[AllowAnonymous]
public sealed class AccountController : Controller
{
    private readonly IAuthService _authService;
    private readonly ILogger<AccountController> _logger;

    public AccountController(IAuthService authService, ILogger<AccountController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToLocal(returnUrl);
        }

        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            var tokenResponse = await _authService.LoginAsync(model.Email, model.Password, ct);
            var principal = BuildClaimsPrincipal(tokenResponse.User.UserId,
                                                 tokenResponse.User.Email,
                                                 tokenResponse.User.DisplayName,
                                                 tokenResponse.User.Roles,
                                                 tokenResponse.AccessToken,
                                                 tokenResponse.RefreshToken);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                new AuthenticationProperties { IsPersistent = false });

            _logger.LogInformation("User {Email} signed in successfully.", model.Email);
            return RedirectToLocal(model.ReturnUrl);
        }
        catch (ApiException ex)
        {
            _logger.LogWarning("Login failed for {Email}: {Message}", model.Email, ex.Message);
            model.ErrorMessage = ex.StatusCode == 401
                ? "Invalid email or password."
                : "Login service is currently unavailable. Please try again.";
            return View(model);
        }
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout(CancellationToken ct)
    {
        var refreshToken = User.FindFirst("refresh_token")?.Value;

        if (!string.IsNullOrWhiteSpace(refreshToken))
        {
            try
            {
                await _authService.LogoutAsync(refreshToken, ct);
            }
            catch (ApiException ex)
            {
                _logger.LogWarning("Remote logout failed: {Message}", ex.Message);
                // Proceed with local sign-out regardless
            }
        }

        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    public IActionResult AccessDenied() => View();

    // ── Helpers ────────────────────────────────────────────────────────────────

    private static ClaimsPrincipal BuildClaimsPrincipal(
        string userId,
        string email,
        string displayName,
        IEnumerable<string> roles,
        string accessToken,
        string refreshToken)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId),
            new(ClaimTypes.Email, email),
            new(ClaimTypes.Name, displayName),
            // Store JWT so the TokenDelegatingHandler can forward it
            new(ClaimTypes.UserData, accessToken),
            new("refresh_token", refreshToken),
        };

        claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        return new ClaimsPrincipal(identity);
    }

    private IActionResult RedirectToLocal(string? returnUrl)
    {
        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction("Index", "Dashboard");
    }
}
