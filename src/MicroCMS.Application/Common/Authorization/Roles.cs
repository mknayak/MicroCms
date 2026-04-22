namespace MicroCMS.Application.Common.Authorization;

/// <summary>
/// Built-in role name constants. Matches the <c>role</c> JWT claim values issued by the
/// identity service and stored on <see cref="../Interfaces/ICurrentUser.Roles"/>.
/// </summary>
public static class Roles
{
    public const string SystemAdmin  = "SystemAdmin";
    public const string TenantAdmin  = "TenantAdmin";
    public const string Editor       = "Editor";
    public const string Author       = "Author";
    public const string Viewer       = "Viewer";
}
