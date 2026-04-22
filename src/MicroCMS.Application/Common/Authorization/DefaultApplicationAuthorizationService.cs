using MicroCMS.Application.Common.Interfaces;

namespace MicroCMS.Application.Common.Authorization;

/// <summary>
/// Default implementation of <see cref="IApplicationAuthorizationService"/>.
/// Grants access when the current user holds at least one role that satisfies every required policy,
/// using <see cref="RolePermissions"/> as the role-to-policy mapping source.
///
/// Security properties:
/// - Unauthenticated users are always denied (fail-secure).
/// - All policies must be satisfied; partial satisfaction is denied.
/// </summary>
public sealed class DefaultApplicationAuthorizationService(ICurrentUser currentUser)
    : IApplicationAuthorizationService
{
    public bool IsAuthorized(params string[] policies)
    {
        if (!currentUser.IsAuthenticated)
        {
            return false;
        }

        foreach (var policy in policies)
        {
            if (!RolePermissions.IsGranted(currentUser.Roles, policy))
            {
                return false;
            }
        }

        return true;
    }
}
