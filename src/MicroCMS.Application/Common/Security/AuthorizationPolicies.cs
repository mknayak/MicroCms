namespace MicroCMS.Application.Common.Security;

/// <summary>
/// Central registry of all authorization policy names used across the MicroCMS application.
/// Declaring them here prevents magic-string duplication and ensures a single source of truth
/// for both the policy-registration code (WebHost) and the <c>[Authorize(Policy = ...)]</c>
/// attributes on controllers/endpoints.
/// </summary>
public static class AuthorizationPolicies
{
    // ── Tenant-level policies ─────────────────────────────────────────────

    /// <summary>User is authenticated and belongs to the current tenant.</summary>
    public const string TenantMember = "TenantMember";

    /// <summary>User holds the TenantAdmin workflow role tenant-wide.</summary>
    public const string TenantAdmin = "TenantAdmin";

    // ── Content workflow policies ─────────────────────────────────────────

    /// <summary>User can create and edit draft entries (Author or above).</summary>
    public const string ContentAuthor = "ContentAuthor";

    /// <summary>User can edit and submit entries for review (Editor or above).</summary>
    public const string ContentEditor = "ContentEditor";

    /// <summary>User can approve entries for publication (Approver or above).</summary>
    public const string ContentApprover = "ContentApprover";

    /// <summary>User can publish and unpublish entries (Publisher or TenantAdmin).</summary>
    public const string ContentPublisher = "ContentPublisher";

    // ── API key policy ────────────────────────────────────────────────────

    /// <summary>Request is authenticated via a valid API key (server-to-server).</summary>
    public const string ApiKey = "ApiKey";

    // ── System-level policies ─────────────────────────────────────────────

    /// <summary>Request originates from the internal service mesh (used by plugin host).</summary>
    public const string InternalService = "InternalService";
}
