namespace MicroCMS.Application.Common.Authorization;

/// <summary>
/// Maps built-in role names to the set of policies they satisfy.
/// <see cref="DefaultApplicationAuthorizationService"/> uses this mapping to evaluate
/// <see cref="../Attributes/HasPolicyAttribute"/> declarations.
///
/// Each role is additive; a user with multiple roles holds the union of all their policies.
/// </summary>
public static class RolePermissions
{
    private static readonly Dictionary<string, HashSet<string>> _map = new(StringComparer.OrdinalIgnoreCase)
    {
        [Roles.SystemAdmin] = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ContentPolicies.EntryRead,
            ContentPolicies.EntryCreate,
            ContentPolicies.EntryUpdate,
            ContentPolicies.EntryDelete,
            ContentPolicies.EntryPublish,
            ContentPolicies.EntrySchedule,
            ContentPolicies.EntryReview,
            ContentPolicies.EntryExport,
            ContentPolicies.FolderManage,
            ContentPolicies.ContentTypeRead,
            ContentPolicies.ContentTypeManage,
            ContentPolicies.MediaRead,
            ContentPolicies.MediaUpload,
            ContentPolicies.MediaDelete,
            ContentPolicies.TaxonomyRead,
            ContentPolicies.TaxonomyManage,
            ContentPolicies.TenantManage,
            ContentPolicies.UserManage,
            ContentPolicies.SystemAdmin,
        },

        [Roles.TenantAdmin] = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ContentPolicies.EntryRead,
            ContentPolicies.EntryCreate,
            ContentPolicies.EntryUpdate,
            ContentPolicies.EntryDelete,
            ContentPolicies.EntryPublish,
            ContentPolicies.EntrySchedule,
            ContentPolicies.EntryReview,
            ContentPolicies.EntryExport,
            ContentPolicies.FolderManage,
            ContentPolicies.ContentTypeRead,
            ContentPolicies.ContentTypeManage,
            ContentPolicies.MediaRead,
            ContentPolicies.MediaUpload,
            ContentPolicies.MediaDelete,
            ContentPolicies.TaxonomyRead,
            ContentPolicies.TaxonomyManage,
            ContentPolicies.TenantManage,
            ContentPolicies.UserManage,
        },

        [Roles.Editor] = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ContentPolicies.EntryRead,
            ContentPolicies.EntryCreate,
            ContentPolicies.EntryUpdate,
            ContentPolicies.EntryPublish,
            ContentPolicies.EntrySchedule,
            ContentPolicies.EntryExport,
            ContentPolicies.FolderManage,
            ContentPolicies.ContentTypeRead,
            ContentPolicies.MediaRead,
            ContentPolicies.MediaUpload,
            ContentPolicies.TaxonomyRead,
        },

        [Roles.Approver] = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ContentPolicies.EntryRead,
            ContentPolicies.EntryReview,
            ContentPolicies.ContentTypeRead,
            ContentPolicies.MediaRead,
            ContentPolicies.TaxonomyRead,
        },

        [Roles.Author] = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ContentPolicies.EntryRead,
            ContentPolicies.EntryCreate,
            ContentPolicies.EntryUpdate,
            ContentPolicies.ContentTypeRead,
            ContentPolicies.MediaRead,
            ContentPolicies.MediaUpload,
            ContentPolicies.TaxonomyRead,
        },

        [Roles.Viewer] = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ContentPolicies.EntryRead,
            ContentPolicies.ContentTypeRead,
            ContentPolicies.MediaRead,
            ContentPolicies.TaxonomyRead,
        },
    };

    /// <summary>Returns true if any of the given roles grants the specified policy.</summary>
    public static bool IsGranted(IReadOnlyList<string> roles, string policy)
    {
        foreach (var role in roles)
        {
            if (_map.TryGetValue(role, out var policies) && policies.Contains(policy))
            {
                return true;
            }
        }

        return false;
    }
}
