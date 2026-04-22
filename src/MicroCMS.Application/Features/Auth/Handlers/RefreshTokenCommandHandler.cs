using MediatR;
using MicroCMS.Application.Common.Exceptions;
using MicroCMS.Application.Common.Interfaces;
using MicroCMS.Application.Features.Auth.Commands;
using MicroCMS.Application.Features.Auth.Dtos;
using MicroCMS.Domain.Aggregates.Identity;
using MicroCMS.Domain.Repositories;
using MicroCMS.Domain.Specifications.Identity;
using MicroCMS.Shared.Ids;
using MicroCMS.Shared.Results;

namespace MicroCMS.Application.Features.Auth.Handlers;

/// <summary>
/// Rotates a refresh token: consumes the presented token, issues a new pair.
/// If the presented token belongs to an already-consumed family (replay attack detected),
/// all tokens in that family are immediately revoked.
/// </summary>
internal sealed class RefreshTokenCommandHandler(
    IRepository<User, UserId> userRepo,
    IRepository<RefreshToken, RefreshTokenId> tokenRepo,
    ITokenService tokenService,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTime) : IRequestHandler<RefreshTokenCommand, Result<AuthTokenResponse>>
{
    private static readonly TimeSpan RefreshTokenLifetime = TimeSpan.FromDays(7);

    public async Task<Result<AuthTokenResponse>> Handle(
        RefreshTokenCommand request,
        CancellationToken cancellationToken)
    {
        var hash = tokenService.HashToken(request.RefreshToken);
        var tokens = await tokenRepo.ListAsync(new RefreshTokenByHashSpec(hash), cancellationToken);
        var token = tokens.FirstOrDefault()
            ?? throw new UnauthorizedException("Refresh token not found.");

        // Replay detection: token was already consumed — revoke the whole family
        if (token.IsRevoked)
        {
            await RevokeFamily(token.FamilyId, tokenRepo, unitOfWork, cancellationToken);
            throw new UnauthorizedException("Refresh token has already been used. All sessions have been revoked.");
        }

        if (token.IsExpired())
            throw new UnauthorizedException("Refresh token has expired. Please log in again.");

        // Load user
        var user = await userRepo.GetByIdAsync(token.UserId, cancellationToken)
            ?? throw new UnauthorizedException("User not found.");

        if (!user.IsActive)
            throw new UnauthorizedException("User account is inactive.");

        // Generate new pair
        var accessToken = tokenService.GenerateAccessToken(user);
        var (rawNewRefresh, newRefreshHash) = tokenService.GenerateRefreshToken();
        var newExpiry = dateTime.UtcNow.Add(RefreshTokenLifetime);

        var newToken = RefreshToken.CreateRotated(user.Id, user.TenantId, newRefreshHash, token.FamilyId, newExpiry);
        token.Consume(newRefreshHash);

        tokenRepo.Update(token);
        await tokenRepo.AddAsync(newToken, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var accessExpiry = dateTime.UtcNow.AddMinutes(15);
        var userDto = new AuthUserDto(
            user.Id.Value,
            user.Email.Value,
            user.DisplayName.Value,
            user.Roles.Select(r => r.WorkflowRole.ToString()).ToList().AsReadOnly());

        return Result.Success(new AuthTokenResponse(accessToken, rawNewRefresh, accessExpiry, newExpiry, userDto));
    }

    private static async Task RevokeFamily(
        Guid familyId,
        IRepository<RefreshToken, RefreshTokenId> tokenRepo,
        IUnitOfWork unitOfWork,
        CancellationToken cancellationToken)
    {
        var familyTokens = await tokenRepo.ListAsync(new ActiveTokensByFamilySpec(familyId), cancellationToken);
        foreach (var t in familyTokens)
        {
            t.Revoke();
            tokenRepo.Update(t);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
