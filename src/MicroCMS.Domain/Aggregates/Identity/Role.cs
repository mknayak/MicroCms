using MicroCMS.Domain.Entities;
using MicroCMS.Domain.Enums;
using MicroCMS.Domain.Exceptions;
using MicroCMS.Shared.Ids;

namespace MicroCMS.Domain.Aggregates.Identity;

/// <summary>
/// Role entity scoped to a tenant. Wraps a <see cref="WorkflowRole"/> with
/// a display name and an optional per-site scope restriction.
/// </summary>
public sealed class Role : Entity<RoleId>
{
    public const int MaxNameLength = 100;

    private Role() { } // EF Core

    internal Role(RoleId id, TenantId tenantId, WorkflowRole workflowRole, string name, SiteId? siteId)
        : base(id)
    {
        TenantId = tenantId;
        WorkflowRole = workflowRole;
        Name = name;
        SiteId = siteId;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public TenantId TenantId { get; private set; }
    public WorkflowRole WorkflowRole { get; private set; }
    public string Name { get; private set; } = string.Empty;

    /// <summary>When set, this role applies only to the specified site.</summary>
    public SiteId? SiteId { get; private set; }

    public bool IsTenantWide => SiteId is null;
    public DateTimeOffset CreatedAt { get; private set; }

    internal static Role Create(
        TenantId tenantId,
        WorkflowRole workflowRole,
        string name,
        SiteId? siteId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name, nameof(name));

        if (name.Length > MaxNameLength)
        {
            throw new DomainException($"Role name must not exceed {MaxNameLength} characters.");
        }

        return new Role(RoleId.New(), tenantId, workflowRole, name.Trim(), siteId);
    }
}
