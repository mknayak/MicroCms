using MediatR;
using MicroCMS.Application.Common.Exceptions;
using MicroCMS.Application.Common.Interfaces;
using MicroCMS.Application.Features.Install.Commands;
using MicroCMS.Application.Features.Install.Dtos;
using MicroCMS.Application.Services;
using MicroCMS.Domain.Aggregates.Identity;
using MicroCMS.Domain.Repositories;
using MicroCMS.Shared.Ids;
using MicroCMS.Shared.Results;

namespace MicroCMS.Application.Features.Install.Handlers;

/// <summary>
/// Handles the first-run installation flow:
///   1. Verifies the system has not already been installed.
///   2. Provisions the first tenant + default site + admin user (via <see cref="ITenantOnboardingService"/>).
///   3. Sets the admin user's password immediately in the same transaction.
///   4. Marks the system as installed in the <see cref="IInstallationStateService"/> cache.
///
/// This handler deliberately bypasses the <c>[HasPolicy]</c> authorization attribute
/// that protects <c>OnboardTenantCommand</c> — it is guarded instead by the
/// <c>InstallationGuardMiddleware</c> which rejects install calls once the system
/// is already installed.
/// </summary>
internal sealed class InstallSystemCommandHandler(
    ITenantOnboardingService onboardingService,
    IRepository<User, UserId> userRepo,
    IPasswordHasher passwordHasher,
    IInstallationStateService installationState,
  IUnitOfWork unitOfWork) : IRequestHandler<InstallSystemCommand, Result<InstallResultDto>>
{
    public async Task<Result<InstallResultDto>> Handle(
        InstallSystemCommand request,
        CancellationToken cancellationToken)
    {
 // Double-check: guard against race condition between two concurrent install requests
        if (await installationState.IsInstalledAsync(cancellationToken))
  throw new ConflictException("System", "The system has already been installed.");

        // Step 1 — provision tenant + site + admin user (no password yet)
        var onboardRequest = new TenantOnboardingRequest(
      Slug: request.TenantSlug,
       DisplayName: request.TenantDisplayName,
    DefaultLocale: request.DefaultLocale,
    TimeZoneId: request.TimeZoneId,
            AdminEmail: request.AdminEmail,
          AdminDisplayName: request.AdminDisplayName,
            DefaultSiteName: request.DefaultSiteName);

        var onboardResult = await onboardingService.OnboardAsync(onboardRequest, cancellationToken);

        // Step 2 — load the new admin user and set their password in the same UoW scope
      var adminUser = await userRepo.GetByIdAsync(new UserId(onboardResult.AdminUserId), cancellationToken)
       ?? throw new NotFoundException(nameof(User), onboardResult.AdminUserId);

        var hash = passwordHasher.Hash(request.AdminPassword);
        adminUser.SetPasswordHash(hash);
  userRepo.Update(adminUser);

        // Step 3 — commit everything atomically
        await unitOfWork.SaveChangesAsync(cancellationToken);

        // Step 4 — prime the in-memory installed flag so no more DB checks are needed
        installationState.MarkInstalled();

     return Result.Success(new InstallResultDto(
      TenantId: onboardResult.TenantId,
      SiteId: onboardResult.DefaultSiteId,
     AdminUserId: onboardResult.AdminUserId,
            AdminEmail: request.AdminEmail,
          Message: "System installed successfully. You can now log in."));
  }
}
