namespace MicroCMS.Application.Common.Interfaces;

/// <summary>
/// Evaluates whether the current user is permitted to execute an operation.
/// Implementations live in the Application layer and rely solely on
/// <see cref="ICurrentUser"/> to remain infrastructure-free.
/// </summary>
public interface IApplicationAuthorizationService
{
    /// <summary>
    /// Returns <c>true</c> when the authenticated user holds all of the supplied policies.
    /// </summary>
    bool IsAuthorized(params string[] policies);
}
