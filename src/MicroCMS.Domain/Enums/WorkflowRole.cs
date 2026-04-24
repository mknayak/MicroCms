namespace MicroCMS.Domain.Enums;

/// <summary>
/// Editorial workflow roles (FR-CM-10).
/// These roles control which entry state transitions a user may trigger.
/// </summary>
public enum WorkflowRole
{
    /// <summary>Can create and edit draft entries.</summary>
    Author = 0,

    /// <summary>Can review, comment, and return entries for revision.</summary>
    Editor = 1,

    /// <summary>Can approve entries for publishing.</summary>
    Approver = 2,

    /// <summary>Can publish and unpublish approved entries.</summary>
    Publisher = 3,

    /// <summary>Full control within a tenant: manage users, settings, content types.</summary>
    TenantAdmin = 4,

    /// <summary>System-level super-admin: cross-tenant operations.</summary>
    SystemAdmin = 5,

    /// <summary>Read-only access to published content.</summary>
    Viewer = 6,
}
