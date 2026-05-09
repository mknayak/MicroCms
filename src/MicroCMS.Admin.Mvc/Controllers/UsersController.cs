using MicroCMS.Admin.Mvc.Infrastructure;
using MicroCMS.Admin.Mvc.Models.ApiDtos;
using MicroCMS.Admin.Mvc.Models.ViewModels.Users;
using MicroCMS.Admin.Mvc.Services.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MicroCMS.Admin.Mvc.Controllers;

/// <summary>
/// User management — invite, manage roles, activate/deactivate.
/// Restricted to SystemAdmin and TenantAdmin roles.
/// </summary>
[Authorize(Roles = "SystemAdmin,TenantAdmin")]
public sealed class UsersController : BaseAdminController
{
    private readonly IUserService _userService;
    private readonly ILogger<UsersController> _logger;

    public UsersController(IUserService userService, ILogger<UsersController> logger)
    {
        _userService = userService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index(int page = 1, CancellationToken ct = default)
    {
        try
        {
            var result = await _userService.ListAsync(page, pageSize: 20, ct);
            var model = new UsersViewModel
            {
                Items = result.Items,
                TotalCount = result.TotalCount,
                PageNumber = result.PageNumber,
                PageSize = result.PageSize,
                TotalPages = result.TotalPages,
            };
            return View(model);
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "Failed to list users.");
            AddError("Unable to load users.");
            return View(new UsersViewModel());
        }
    }

    [HttpGet]
    public IActionResult Invite() => View(new InviteUserViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Invite(InviteUserViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            await _userService.InviteAsync(new InviteUserRequest { Email = model.Email, DisplayName = model.DisplayName }, ct);
            AddSuccess($"Invitation sent to {model.Email}.");
            return RedirectToAction(nameof(Index));
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "Failed to invite user {Email}.", model.Email);
            HandleApiError(ex);
            return View(model);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Deactivate(string id, CancellationToken ct)
    {
        try
        {
            await _userService.DeactivateAsync(id, ct);
            AddSuccess("User deactivated.");
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "Failed to deactivate user {Id}.", id);
            AddError(ex.Message);
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Activate(string id, CancellationToken ct)
    {
        try
        {
            await _userService.ActivateAsync(id, ct);
            AddSuccess("User activated.");
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "Failed to activate user {Id}.", id);
            AddError(ex.Message);
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "SystemAdmin")]
    public async Task<IActionResult> Delete(string id, CancellationToken ct)
    {
        try
        {
            await _userService.DeleteAsync(id, ct);
            AddSuccess("User deleted.");
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "Failed to delete user {Id}.", id);
            AddError(ex.Message);
        }

        return RedirectToAction(nameof(Index));
    }
}
