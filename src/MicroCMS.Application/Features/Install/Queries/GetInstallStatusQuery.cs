using MicroCMS.Application.Common.Markers;
using MicroCMS.Application.Features.Install.Dtos;

namespace MicroCMS.Application.Features.Install.Queries;

/// <summary>
/// Returns the current installation status of the system.
/// Safe to call anonymously — no sensitive data is exposed.
/// </summary>
public sealed record GetInstallStatusQuery : IQuery<InstallStatusDto>;
