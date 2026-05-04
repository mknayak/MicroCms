using MicroCMS.Shared.Ids;

namespace MicroCMS.Ai.Abstractions.Interfaces;

/// <summary>
/// Provides current authenticated user context for AI operations.
/// Minimal interface to avoid circular dependencies.
/// </summary>
public interface IAiCurrentUser
{
    Guid UserId { get; }
    TenantId TenantId { get; }
    SiteId? SiteId { get; }
}
