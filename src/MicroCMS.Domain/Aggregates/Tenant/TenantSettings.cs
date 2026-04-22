using MicroCMS.Domain.Exceptions;
using MicroCMS.Shared.Ids;

namespace MicroCMS.Domain.Aggregates.Tenant;

/// <summary>Strongly-typed ID for <c>TenantSecuritySettings</c>.</summary>
public readonly record struct TenantSecuritySettingsId(Guid Value)
{
    public static TenantSecuritySettingsId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString();
}

/// <summary>
/// Tenant-wide security and session configuration aggregate (GAP-28).
/// Covers: MFA policy, SSO/OIDC, session timeouts, and IP allowlist.
/// Mutable properties (unlike the <see cref="MicroCMS.Domain.ValueObjects.TenantSettings"/> value object).
/// </summary>
public sealed class TenantSecuritySettings : AggregateRoot<TenantSecuritySettingsId>
{
    private readonly List<string> _ipAllowlist = [];

    private TenantSecuritySettings() : base() { }

    private TenantSecuritySettings(TenantSecuritySettingsId id, TenantId tenantId)
        : base(id)
    {
        TenantId = tenantId;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public TenantId TenantId { get; private set; }
    public bool RequireMfaForAdmins { get; private set; }
    public TimeSpan SessionIdleTimeout { get; private set; } = TimeSpan.FromMinutes(30);
    public TimeSpan AbsoluteSessionTimeout { get; private set; } = TimeSpan.FromHours(8);
    public bool SsoEnabled { get; private set; }
    public string? OidcIssuer { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public IReadOnlyList<string> IpAllowlist => _ipAllowlist.AsReadOnly();

    public static TenantSecuritySettings CreateDefault(TenantId tenantId) =>
        new(TenantSecuritySettingsId.New(), tenantId);

    public void Update(
        bool requireMfaForAdmins,
        TimeSpan sessionIdleTimeout,
        TimeSpan absoluteSessionTimeout,
        bool ssoEnabled,
        string? oidcIssuer,
        IEnumerable<string>? ipAllowlist)
    {
        if (sessionIdleTimeout <= TimeSpan.Zero)
            throw new DomainException("Session idle timeout must be positive.");
        if (absoluteSessionTimeout <= TimeSpan.Zero)
            throw new DomainException("Absolute session timeout must be positive.");

        RequireMfaForAdmins = requireMfaForAdmins;
        SessionIdleTimeout = sessionIdleTimeout;
        AbsoluteSessionTimeout = absoluteSessionTimeout;
        SsoEnabled = ssoEnabled;
        OidcIssuer = oidcIssuer?.Trim();

        _ipAllowlist.Clear();
        if (ipAllowlist is not null)
            _ipAllowlist.AddRange(
                ipAllowlist.Select(ip => ip.Trim()).Where(ip => ip.Length > 0).Distinct());

        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
