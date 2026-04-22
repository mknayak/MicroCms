using MediatR;
using MicroCMS.Application.Features.Tenants.Commands;
using MicroCMS.Application.Services;
using MicroCMS.Shared.Results;

namespace MicroCMS.Application.Features.Tenants.Handlers;

internal sealed class OnboardTenantCommandHandler(
    ITenantOnboardingService onboardingService)
    : IRequestHandler<OnboardTenantCommand, Result<TenantOnboardingResult>>
{
    public async Task<Result<TenantOnboardingResult>> Handle(
   OnboardTenantCommand request,
        CancellationToken cancellationToken)
    {
        var result = await onboardingService.OnboardAsync(
    new TenantOnboardingRequest(
                request.Slug,
     request.DisplayName,
           request.DefaultLocale,
   request.TimeZoneId,
                request.AdminEmail,
  request.AdminDisplayName,
  request.DefaultSiteName),
          cancellationToken);

        return Result.Success(result);
    }
}
