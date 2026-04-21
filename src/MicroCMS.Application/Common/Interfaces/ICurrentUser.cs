using MicroCMS.Shared.Ids;

namespace MicroCMS.Application.Common.Interfaces;

/// <summary>
/// Provides the identity of the authenticated user for the current request.
/// Resolved from JWT claims by the Infrastructure layer.
/// </summary>
public interface ICurrentUser
{
    Guid UserId { get; }
    TenantId TenantId { get; }
    string? Email { get; }
    IReadOnlyList<string> Roles { get; }
    bool IsAuthenticated { get; }
}
