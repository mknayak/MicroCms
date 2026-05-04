using MediatR;
using MicroCMS.Application.Common.Exceptions;
using MicroCMS.Application.Common.Interfaces;
using MicroCMS.Application.Features.Auth.Commands;
using MicroCMS.Application.Features.Auth.Dtos;
using MicroCMS.Domain.Aggregates.Identity;
using MicroCMS.Domain.Repositories;
using MicroCMS.Shared.Ids;
using MicroCMS.Shared.Results;

namespace MicroCMS.Application.Features.Auth.Handlers;

/// <summary>
/// Issues a new access + refresh token pair scoped to the requested site.
/// Validates that the current user has at least one role on the target site
/// (or is a tenant-wide admin) before embedding the <c>site_id</c> claim.
/// </summary>
internal sealed class SwitchSiteCommandHandler(
    IRepository<User, UserId> userRepo,
    IRepository<RefreshToken, RefreshTokenId> tokenRepo,
    ITokenService tokenService,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTime) : IRequestHandler<SwitchSiteCommand, Result<AuthTokenResponse>>
{
    private static readonly TimeSpan RefreshTokenLifetime = TimeSpan.FromDays(7);

    public async Task<Result<AuthTokenResponse>> Handle(
        SwitchSiteCommand request,
        CancellationToken cancellationToken)
    {
        var targetSiteId = new SiteId(request.SiteId);

        var user = await userRepo.GetByIdAsync(new UserId(currentUser.UserId), cancellationToken)
            ?? throw new UnauthorizedException("User not found.");

        if (!user.IsActive)
            throw new UnauthorizedException("User account is inactive.");

        // Verify the user has access to the requested site:
        // either a tenant-wide role (SiteId == null) or a site-scoped role for this site.
        var hasAccess = user.Roles.Any(r => r.SiteId is null || r.SiteId == targetSiteId);
        if (!hasAccess)
            return Result.Failure<AuthTokenResponse>(
                Error.Forbidden("Auth.SiteAccessDenied",
                    $"You do not have access to site '{request.SiteId}'."));

        var accessToken = tokenService.GenerateAccessToken(user, targetSiteId);
        var (rawRefresh, refreshHash) = tokenService.GenerateRefreshToken();
        var refreshExpiry = dateTime.UtcNow.Add(RefreshTokenLifetime);

        var refreshToken = RefreshToken.CreateNew(user.Id, user.TenantId, refreshHash, refreshExpiry);
        await tokenRepo.AddAsync(refreshToken, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var accessExpiry = dateTime.UtcNow.AddMinutes(15);
        var userDto = new AuthUserDto(
            user.Id.Value,
            user.Email.Value,
            user.DisplayName.Value,
            user.Roles.Select(r => r.WorkflowRole.ToString()).ToList().AsReadOnly());

        return Result.Success(new AuthTokenResponse(
            accessToken, rawRefresh, accessExpiry, refreshExpiry, userDto));
    }
}
