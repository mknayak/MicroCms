namespace MicroCMS.Application.Common.Authorization;

/// <summary>
/// Centralised constants for all application authorization policies.
/// Using constants prevents typo-related policy mismatches and enables compile-time discovery.
///
/// Naming convention: {Aggregate}{Operation}
/// </summary>
public static class ContentPolicies
{
    // ── Entry policies ─────────────────────────────────────────────────────
    public const string EntryRead      = "Entry.Read";
    public const string EntryCreate    = "Entry.Create";
    public const string EntryUpdate    = "Entry.Update";
    public const string EntryDelete    = "Entry.Delete";
    public const string EntryPublish   = "Entry.Publish";
    public const string EntrySchedule  = "Entry.Schedule";
    public const string EntryReview    = "Entry.Review";
    public const string EntryExport    = "Entry.Export";

    // ── Folder policies ────────────────────────────────────────────────────
    public const string FolderManage   = "Folder.Manage";

    // ── ContentType policies ───────────────────────────────────────────────
    public const string ContentTypeRead   = "ContentType.Read";
    public const string ContentTypeManage = "ContentType.Manage";

    // ── Media policies ─────────────────────────────────────────────────────
    public const string MediaRead   = "Media.Read";
    public const string MediaUpload = "Media.Upload";
    public const string MediaDelete = "Media.Delete";

    // ── Taxonomy policies ──────────────────────────────────────────────────
    public const string TaxonomyRead   = "Taxonomy.Read";
    public const string TaxonomyManage = "Taxonomy.Manage";

    // ── Tenant / admin policies ────────────────────────────────────────────
    public const string TenantManage  = "Tenant.Manage";
    public const string UserManage    = "User.Manage";
    public const string SystemAdmin   = "System.Admin";
}
