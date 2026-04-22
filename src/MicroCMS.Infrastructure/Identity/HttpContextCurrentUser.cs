using System.Security.Claims;
using MicroCMS.Application.Common.Interfaces;
using MicroCMS.Shared.Ids;
using Microsoft.AspNetCore.Http;

namespace MicroCMS.Infrastructure.Identity;

/// <summary>
/// Resolves the current authenticated user's identity from the JWT bearer claims
/// via <see cref="IHttpContextAccessor"/>.
///
/// Claim mappings (configurable, but defaults match the JWT spec and ASP.NET Core defaults):
/// - UserId  → <c>sub</c> (Subject) claim
/// - TenantId → <c>tenant_id</c> custom claim
/// - Email   → <c>email</c> claim (OIDC standard)
/// - Roles   → <c>role</c> claims (multiple)
///
/// Security: All values are read from validated JWT claims — never from request headers
/// or query strings, which can be spoofed. The JWT signature is verified by the
/// <c>AddJwtBearer</c> middleware before this service is called.
/// </summary>
internal sealed class HttpContextCurrentUser : ICurrentUser
{
    private const string TenantIdClaimType = "tenant_id";
    private const string SubjectClaimType = ClaimTypes.NameIdentifier;
    private const string EmailClaimType = ClaimTypes.Email;
    private const string RoleClaimType = ClaimTypes.Role;

    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpContextCurrentUser(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

    public bool IsAuthenticated =>
        User?.Identity?.IsAuthenticated is true;

    public Guid UserId
    {
        get
        {
            var claim = User?.FindFirst(SubjectClaimType)?.Value;
            return Guid.TryParse(claim, out var id) ? id : Guid.Empty;
        }
    }

    public TenantId TenantId
    {
        get
        {
            var claim = User?.FindFirst(TenantIdClaimType)?.Value;
            return TenantId.TryParse(claim ?? string.Empty, out var id) ? id : TenantId.Empty;
        }
    }

    public string? Email =>
        User?.FindFirst(EmailClaimType)?.Value;

    public IReadOnlyList<string> Roles =>
        User?.FindAll(RoleClaimType)
            .Select(c => c.Value)
            .ToList()
            .AsReadOnly()
        ?? Array.Empty<string>().AsReadOnly();
}
