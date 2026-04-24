using MicroCMS.Application.Common.Attributes;
using MicroCMS.Application.Common.Markers;
using MicroCMS.Application.Features.Install.Dtos;

namespace MicroCMS.Application.Features.Install.Commands;

/// <summary>
/// Installs the system on first run:
///   1. Runs the full tenant onboarding flow (tenant + default site + admin user).
///   2. Sets the admin user's initial password in the same transaction.
///
/// This command is intentionally <b>unauthenticated</b> — it is only callable when
/// <see cref="IInstallationStateService.IsInstalledAsync"/> returns <c>false</c>.
/// The <c>InstallationGuardMiddleware</c> and <c>InstallController</c> enforce this together.
/// </summary>
[AllowAnonymousRequest]
public sealed record InstallSystemCommand(
    // ── Tenant ────────────────────────────────────────────────────────────
    string TenantSlug,
    string TenantDisplayName,
    string DefaultLocale,
    string TimeZoneId,
    string DefaultSiteName,
    // ── Admin user ────────────────────────────────────────────────────────
    string AdminEmail,
    string AdminDisplayName,
    string AdminPassword) : ICommand<InstallResultDto>;
