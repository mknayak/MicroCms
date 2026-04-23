using MediatR;
using MicroCMS.Application.Common.Interfaces;
using MicroCMS.Application.Features.Install.Dtos;
using MicroCMS.Application.Features.Install.Queries;
using MicroCMS.Shared.Results;

namespace MicroCMS.Application.Features.Install.Handlers;

/// <summary>
/// Returns the current installation status of the system.
/// Delegates entirely to <see cref="IInstallationStateService"/> so no
/// database round-trip occurs during normal (installed) operation.
/// </summary>
internal sealed class GetInstallStatusQueryHandler(
    IInstallationStateService installationState)
    : IRequestHandler<GetInstallStatusQuery, Result<InstallStatusDto>>
{
    public async Task<Result<InstallStatusDto>> Handle(
        GetInstallStatusQuery request,
        CancellationToken cancellationToken)
{
        var installed = await installationState.IsInstalledAsync(cancellationToken);

   var dto = installed
  ? new InstallStatusDto(true,  "System is installed and ready.")
            : new InstallStatusDto(false, "System has not been installed yet. POST /api/v1/install to complete setup.");

        return Result.Success(dto);
    }
}
