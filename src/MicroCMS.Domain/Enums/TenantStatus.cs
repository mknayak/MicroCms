namespace MicroCMS.Domain.Enums;

/// <summary>Lifecycle states of a tenant.</summary>
public enum TenantStatus
{
    /// <summary>DB schema and defaults are being provisioned.</summary>
    Provisioning = 0,

    /// <summary>Fully operational.</summary>
    Active = 1,

    /// <summary>Temporarily disabled — API calls return 503.</summary>
    Suspended = 2,

    /// <summary>Scheduled for deletion; data retention period active.</summary>
    PendingDeletion = 3
}
