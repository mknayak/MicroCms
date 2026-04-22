using MediatR;
using MicroCMS.Application.Common.Exceptions;
using MicroCMS.Application.Common.Interfaces;
using MicroCMS.Application.Features.Auth.Commands;
using MicroCMS.Domain.Aggregates.Identity;
using MicroCMS.Domain.Repositories;
using MicroCMS.Shared.Ids;
using MicroCMS.Shared.Results;

namespace MicroCMS.Application.Features.Auth.Handlers;

/// <summary>
/// Changes the authenticated user's password after verifying the current one.
/// All active refresh tokens are revoked to force re-authentication on all devices.
/// </summary>
internal sealed class ChangePasswordCommandHandler(
    IRepository<User, UserId> userRepo,
    IRepository<RefreshToken, RefreshTokenId> tokenRepo,
    IPasswordHasher passwordHasher,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork) : IRequestHandler<ChangePasswordCommand, Result>
{
    public async Task<Result> Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
    {
        var user = await userRepo.GetByIdAsync(new UserId(currentUser.UserId), cancellationToken)
            ?? throw new NotFoundException(nameof(User), currentUser.UserId);

        if (user.PasswordHash is null || !passwordHasher.Verify(request.CurrentPassword, user.PasswordHash))
            throw new UnauthorizedException("Current password is incorrect.");

        var newHash = passwordHasher.Hash(request.NewPassword);
        user.SetPasswordHash(newHash); // raises UserPasswordChangedEvent internally
        userRepo.Update(user);

        // Revoke all sessions to force re-login on all devices
        var activeTokens = await tokenRepo.ListAsync(
            new Domain.Specifications.Identity.ActiveTokensByUserSpec(user.Id), cancellationToken);
        foreach (var token in activeTokens)
        {
            token.Revoke();
            tokenRepo.Update(token);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

/// <summary>
/// Sets the initial password for a user created via invitation (no existing credential).
/// Only callable by TenantAdmin or by the user themselves through an invite-token flow.
/// </summary>
internal sealed class SetInitialPasswordCommandHandler(
    IRepository<User, UserId> userRepo,
    IPasswordHasher passwordHasher,
    IUnitOfWork unitOfWork) : IRequestHandler<SetInitialPasswordCommand, Result>
{
    public async Task<Result> Handle(SetInitialPasswordCommand request, CancellationToken cancellationToken)
    {
        var user = await userRepo.GetByIdAsync(new UserId(request.UserId), cancellationToken)
            ?? throw new NotFoundException(nameof(User), request.UserId);

        if (user.PasswordHash is not null)
            throw new ConflictException(nameof(User), "Password is already set. Use change-password instead.");

        var hash = passwordHasher.Hash(request.NewPassword);
        user.SetPasswordHash(hash);
        userRepo.Update(user);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
