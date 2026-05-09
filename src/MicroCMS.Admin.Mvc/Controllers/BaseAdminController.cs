using MicroCMS.Admin.Mvc.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MicroCMS.Admin.Mvc.Controllers;

/// <summary>
/// Base controller for all authenticated admin actions.
/// Enforces authentication and provides shared error handling helpers.
/// </summary>
[Authorize]
public abstract class BaseAdminController : Controller
{
    /// <summary>
    /// Adds a temporary success notification to TempData.
    /// </summary>
    protected void AddSuccess(string message) =>
        TempData["SuccessMessage"] = message;

    /// <summary>
    /// Adds a temporary error notification to TempData.
    /// </summary>
    protected void AddError(string message) =>
        TempData["ErrorMessage"] = message;

    /// <summary>
    /// Handles an <see cref="ApiException"/> by adding a model error and
    /// setting TempData, then returns false so the caller can re-render the form.
    /// </summary>
    protected bool HandleApiError(ApiException ex, string? fieldName = null)
    {
        var message = ex.Detail ?? ex.Message;

        if (fieldName is not null)
        {
            ModelState.AddModelError(fieldName, message);
        }
        else
        {
            ModelState.AddModelError(string.Empty, message);
        }

        AddError(message);
        return false;
    }
}
