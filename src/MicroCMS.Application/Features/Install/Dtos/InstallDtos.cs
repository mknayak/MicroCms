namespace MicroCMS.Application.Features.Install.Dtos;

/// <summary>Returned by <c>GET /api/v1/install/status</c>.</summary>
public sealed record InstallStatusDto(
    /// <summary><c>true</c> when the system has been installed and login is possible.</summary>
    bool IsInstalled,
    /// <summary>Human-readable description of the current state.</summary>
    string Message);

/// <summary>Returned by <c>POST /api/v1/install</c> on success.</summary>
public sealed record InstallResultDto(
    Guid TenantId,
    Guid SiteId,
    Guid AdminUserId,
    string AdminEmail,
    string Message);
