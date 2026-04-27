namespace MicroCMS.Application.Common.Authorization;

/// <summary>
/// Built-in role name constants matching the agreed hierarchy:
/// SystemAdmin → SiteAdmin → ContentAdmin → Designer → ContentApprover → ContentAuthor
/// </summary>
public static class Roles
{
    public const string SystemAdmin  = "SystemAdmin";
    public const string SiteAdmin = "SiteAdmin";
    public const string ContentAdmin   = "ContentAdmin";
    public const string Designer       = "Designer";
    public const string ContentApprover = "ContentApprover";
    public const string ContentAuthor  = "ContentAuthor";

    // Legacy aliases kept for backwards compat
    public const string TenantAdmin  = "TenantAdmin";
    public const string Editor     = "Editor";
    public const string Approver     = "Approver";
    public const string Author       = "Author";
    public const string Viewer       = "Viewer";
}
