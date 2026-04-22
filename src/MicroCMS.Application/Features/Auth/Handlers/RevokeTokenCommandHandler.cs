using MediatR;
using MicroCMS.Application.Common.Exceptions;
using MicroCMS.Application.Common.Interfaces;
using MicroCMS.Application.Features.Auth.Commands;
using MicroCMS.Domain.Aggregates.Identity;
using MicroCMS.Domain.Repositories;
using MicroCMS.Domain.Specifications.Identity;
using MicroCMS.Shared.Ids;
using MicroCMS.Shared.Results;

namespace MicroCMS.Application.Features.Auth.Handlers;

/// <summary>Revokes a single refresh token (single-device logout).</summary>
internal sealed class RevokeTokenCommandHandler(
    IRepository<RefreshToken, RefreshTokenId> tokenRepo,
    ITokenService tokenService,
    IUnitOfWork unitOfWork) : IRequestHandler<RevokeTokenCommand, Result>
{
    public async Task<Result> Handle(RevokeTokenCommand request, CancellationToken cancellationToken)
    {
        var hash = tokenService.HashToken(request.RefreshToken);
        var tokens = await tokenRepo.ListAsync(new RefreshTokenByHashSpec(hash), cancellationToken);
        var token = tokens.FirstOrDefault()
            ?? throw new UnauthorizedException("Refresh token not found.");

        if (!token.IsRevoked)
        {
            token.Revoke();
            tokenRepo.Update(token);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return Result.Success();
    }
}

/// <summary>Revokes all active refresh tokens for the authenticated user (all-devices logout).</summary>
internal sealed class RevokeAllTokensCommandHandler(
    IRepository<RefreshToken, RefreshTokenId> tokenRepo,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork) : IRequestHandler<RevokeAllTokensCommand, Result>
{
    public async Task<Result> Handle(RevokeAllTokensCommand request, CancellationToken cancellationToken)
    {
        var userId = new UserId(currentUser.UserId);
        var activeTokens = await tokenRepo.ListAsync(new ActiveTokensByUserSpec(userId), cancellationToken);

        foreach (var token in activeTokens)
        {
            token.Revoke();
            tokenRepo.Update(token);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
