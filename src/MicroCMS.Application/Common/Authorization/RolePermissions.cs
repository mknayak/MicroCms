namespace MicroCMS.Application.Common.Authorization;

/// <summary>
/// Maps built-in role names to the set of policies they satisfy.
/// </summary>
public static class RolePermissions
{
    private static readonly Dictionary<string, HashSet<string>> _map = new(StringComparer.OrdinalIgnoreCase)
    {
        [Roles.SystemAdmin] = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
      {
            ContentPolicies.EntryRead, ContentPolicies.EntryCreate, ContentPolicies.EntryUpdate,
            ContentPolicies.EntryDelete, ContentPolicies.EntryPublish, ContentPolicies.EntrySchedule,
            ContentPolicies.EntryReview, ContentPolicies.EntryExport, ContentPolicies.FolderManage,
            ContentPolicies.ContentTypeRead, ContentPolicies.ContentTypeManage,
            ContentPolicies.MediaRead, ContentPolicies.MediaUpload, ContentPolicies.MediaDelete,
            ContentPolicies.TaxonomyRead, ContentPolicies.TaxonomyManage,
            ContentPolicies.TenantManage, ContentPolicies.UserManage, ContentPolicies.SystemAdmin,
            ContentPolicies.ComponentRead, ContentPolicies.ComponentManage,
            ContentPolicies.LayoutRead, ContentPolicies.LayoutManage,
            ContentPolicies.PageTemplateRead, ContentPolicies.PageTemplateManage,
            ContentPolicies.PackageExport, ContentPolicies.PackageImport,
        },

        [Roles.SiteAdmin] = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ContentPolicies.EntryRead, ContentPolicies.EntryCreate, ContentPolicies.EntryUpdate,
            ContentPolicies.EntryDelete, ContentPolicies.EntryPublish, ContentPolicies.EntrySchedule,
            ContentPolicies.EntryReview, ContentPolicies.EntryExport, ContentPolicies.FolderManage,
            ContentPolicies.ContentTypeRead, ContentPolicies.ContentTypeManage,
            ContentPolicies.MediaRead, ContentPolicies.MediaUpload, ContentPolicies.MediaDelete,
            ContentPolicies.TaxonomyRead, ContentPolicies.TaxonomyManage,
            ContentPolicies.TenantManage, ContentPolicies.UserManage,
            ContentPolicies.ComponentRead, ContentPolicies.ComponentManage,
            ContentPolicies.LayoutRead, ContentPolicies.LayoutManage,
            ContentPolicies.PageTemplateRead, ContentPolicies.PageTemplateManage,
            ContentPolicies.PackageExport, ContentPolicies.PackageImport,
   },

        [Roles.ContentAdmin] = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ContentPolicies.EntryRead, ContentPolicies.EntryCreate, ContentPolicies.EntryUpdate,
            ContentPolicies.EntryDelete, ContentPolicies.EntryPublish, ContentPolicies.EntrySchedule,
            ContentPolicies.EntryReview, ContentPolicies.EntryExport, ContentPolicies.FolderManage,
            ContentPolicies.ContentTypeRead, ContentPolicies.ContentTypeManage,
            ContentPolicies.MediaRead, ContentPolicies.MediaUpload, ContentPolicies.MediaDelete,
            ContentPolicies.TaxonomyRead, ContentPolicies.TaxonomyManage,
            ContentPolicies.ComponentRead, ContentPolicies.ComponentManage,
            ContentPolicies.LayoutRead,
            ContentPolicies.PageTemplateRead,
        },

        [Roles.Designer] = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ContentPolicies.EntryRead, ContentPolicies.ContentTypeRead,
            ContentPolicies.MediaRead, ContentPolicies.TaxonomyRead,
            ContentPolicies.ComponentRead, ContentPolicies.ComponentManage,
            ContentPolicies.LayoutRead, ContentPolicies.LayoutManage,
            ContentPolicies.PageTemplateRead, ContentPolicies.PageTemplateManage,
        },

        [Roles.ContentApprover] = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ContentPolicies.EntryRead, ContentPolicies.EntryReview, ContentPolicies.EntryPublish,
            ContentPolicies.ContentTypeRead, ContentPolicies.MediaRead,
            ContentPolicies.TaxonomyRead, ContentPolicies.ComponentRead, ContentPolicies.LayoutRead,
            ContentPolicies.PageTemplateRead,
        },

        [Roles.ContentAuthor] = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ContentPolicies.EntryRead, ContentPolicies.EntryCreate, ContentPolicies.EntryUpdate,
            ContentPolicies.ContentTypeRead, ContentPolicies.MediaRead, ContentPolicies.MediaUpload,
            ContentPolicies.TaxonomyRead, ContentPolicies.ComponentRead, ContentPolicies.LayoutRead,
            ContentPolicies.PageTemplateRead,
        },

        // Legacy role aliases — kept for backwards compat
        [Roles.TenantAdmin] = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ContentPolicies.EntryRead, ContentPolicies.EntryCreate, ContentPolicies.EntryUpdate,
            ContentPolicies.EntryDelete, ContentPolicies.EntryPublish, ContentPolicies.EntrySchedule,
            ContentPolicies.EntryReview, ContentPolicies.EntryExport, ContentPolicies.FolderManage,
            ContentPolicies.ContentTypeRead, ContentPolicies.ContentTypeManage,
            ContentPolicies.MediaRead, ContentPolicies.MediaUpload, ContentPolicies.MediaDelete,
            ContentPolicies.TaxonomyRead, ContentPolicies.TaxonomyManage,
            ContentPolicies.TenantManage, ContentPolicies.UserManage,
            ContentPolicies.ComponentRead, ContentPolicies.ComponentManage,
            ContentPolicies.LayoutRead, ContentPolicies.LayoutManage,
            ContentPolicies.PageTemplateRead, ContentPolicies.PageTemplateManage,
            ContentPolicies.PackageExport, ContentPolicies.PackageImport,
        },
        [Roles.Editor] = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ContentPolicies.EntryRead, ContentPolicies.EntryCreate, ContentPolicies.EntryUpdate,
            ContentPolicies.EntryPublish, ContentPolicies.EntrySchedule, ContentPolicies.EntryExport,
            ContentPolicies.FolderManage, ContentPolicies.ContentTypeRead,
            ContentPolicies.MediaRead, ContentPolicies.MediaUpload,
            ContentPolicies.TaxonomyRead, ContentPolicies.ComponentRead, ContentPolicies.ComponentManage,
            ContentPolicies.LayoutRead, ContentPolicies.LayoutManage,
            ContentPolicies.PageTemplateRead, ContentPolicies.PageTemplateManage,
        },
        [Roles.Approver] = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ContentPolicies.EntryRead, ContentPolicies.EntryReview,
            ContentPolicies.ContentTypeRead, ContentPolicies.MediaRead,
            ContentPolicies.TaxonomyRead, ContentPolicies.ComponentRead, ContentPolicies.LayoutRead,
            ContentPolicies.PageTemplateRead,
        },
        [Roles.Author] = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ContentPolicies.EntryRead, ContentPolicies.EntryCreate, ContentPolicies.EntryUpdate,
            ContentPolicies.ContentTypeRead, ContentPolicies.MediaRead, ContentPolicies.MediaUpload,
            ContentPolicies.TaxonomyRead, ContentPolicies.ComponentRead, ContentPolicies.LayoutRead,
            ContentPolicies.PageTemplateRead,
        },
        [Roles.Viewer] = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ContentPolicies.EntryRead, ContentPolicies.ContentTypeRead,
            ContentPolicies.MediaRead, ContentPolicies.TaxonomyRead,
            ContentPolicies.ComponentRead, ContentPolicies.LayoutRead,
            ContentPolicies.PageTemplateRead,
     },
    };

    /// <summary>Returns true if any of the given roles grants the specified policy.</summary>
    public static bool IsGranted(IReadOnlyList<string> roles, string policy)
    {
        foreach (var role in roles)
        {
            if (_map.TryGetValue(role, out var policies) && policies.Contains(policy))
                return true;
        }
        return false;
    }
}
