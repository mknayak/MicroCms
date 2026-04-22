using MicroCMS.Domain.Enums;
using MicroCMS.Domain.Events.Identity;
using MicroCMS.Domain.Exceptions;
using MicroCMS.Domain.ValueObjects;
using MicroCMS.Shared.Ids;

namespace MicroCMS.Domain.Aggregates.Identity;

/// <summary>
/// User aggregate root, scoped to a tenant.
/// Users are identified by their email address within a tenant.
/// Role assignments determine what content workflow operations they may perform.
/// </summary>
public sealed class User : AggregateRoot<UserId>
{
    private readonly List<Role> _roles = [];

    private User() : base() { } // EF Core

    private User(
        UserId id,
        TenantId tenantId,
        EmailAddress email,
        PersonName displayName) : base(id)
    {
        TenantId = tenantId;
        Email = email;
        DisplayName = displayName;
        IsActive = true;
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public TenantId TenantId { get; private set; }
    public EmailAddress Email { get; private set; } = null!;
    public PersonName DisplayName { get; private set; } = null!;
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public IReadOnlyList<Role> Roles => _roles.AsReadOnly();

    // ── Factory ────────────────────────────────────────────────────────────

    public static User Create(TenantId tenantId, EmailAddress email, PersonName displayName)
    {
        var user = new User(UserId.New(), tenantId, email, displayName);
        user.RaiseDomainEvent(new UserCreatedEvent(user.Id, tenantId, email.Value));
        return user;
    }

    // ── Role management ───────────────────────────────────────────────────

    public Role AssignRole(WorkflowRole workflowRole, string roleName, SiteId? siteId = null)
    {
        EnsureActive();

        if (_roles.Any(r => r.WorkflowRole == workflowRole && r.SiteId == siteId))
        {
            throw new BusinessRuleViolationException(
                "User.RoleAlreadyAssigned",
                $"User already has the '{workflowRole}' role{(siteId.HasValue ? $" on site '{siteId}'" : " tenant-wide")}.");
        }

        var role = Role.Create(TenantId, workflowRole, roleName, siteId);
        _roles.Add(role);
        UpdatedAt = DateTimeOffset.UtcNow;
        RaiseDomainEvent(new UserRoleAssignedEvent(Id, TenantId, workflowRole, siteId));
        return role;
    }

    public void RevokeRole(RoleId roleId)
    {
        EnsureActive();
        var role = _roles.FirstOrDefault(r => r.Id == roleId)
            ?? throw new DomainException($"Role '{roleId}' not found on user '{Id}'.");

        _roles.Remove(role);
        UpdatedAt = DateTimeOffset.UtcNow;
        RaiseDomainEvent(new UserRoleRevokedEvent(Id, TenantId, role.WorkflowRole));
    }

    // ── State ─────────────────────────────────────────────────────────────

    public void Deactivate()
    {
        if (!IsActive)
        {
            throw new BusinessRuleViolationException("User.AlreadyInactive", "User is already inactive.");
        }

        IsActive = false;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Reactivate()
    {
        if (IsActive)
        {
            throw new BusinessRuleViolationException("User.AlreadyActive", "User is already active.");
        }

        IsActive = true;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void UpdateDisplayName(PersonName newName)
    {
        ArgumentNullException.ThrowIfNull(newName, nameof(newName));
        DisplayName = newName;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public bool HasRole(WorkflowRole role, SiteId? siteId = null) =>
        _roles.Any(r => r.WorkflowRole == role && (r.IsTenantWide || r.SiteId == siteId));

    // ── Private helpers ───────────────────────────────────────────────────

    private void EnsureActive()
    {
        if (!IsActive)
        {
            throw new BusinessRuleViolationException("User.Inactive", "Cannot modify an inactive user.");
        }
    }
}
