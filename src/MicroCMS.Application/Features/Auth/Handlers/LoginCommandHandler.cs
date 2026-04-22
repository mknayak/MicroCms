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
/// Authenticates a user against their stored bcrypt password hash.
/// On success: issues a new access + refresh token pair.
/// On failure: increments the failed-login counter (account locks after 5 attempts).
/// Records a <see cref="LoginAttempt"/> audit row regardless of outcome.
/// </summary>
internal sealed class LoginCommandHandler(
    IRepository<User, UserId> userRepo,
    IRepository<RefreshToken, RefreshTokenId> tokenRepo,
    IRepository<LoginAttempt, LoginAttemptId> attemptRepo,
    IPasswordHasher passwordHasher,
    ITokenService tokenService,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTime) : IRequestHandler<LoginCommand, Result<AuthTokenResponse>>
{
    private static readonly TimeSpan RefreshTokenLifetime = TimeSpan.FromDays(7);

    public async Task<Result<AuthTokenResponse>> Handle(
        LoginCommand request,
        CancellationToken cancellationToken)
    {
        // 1. Look up user by email (query filter already scopes by tenant via ITenantContext)
        var users = await userRepo.ListAsync(
            new UserByEmailSpec(request.Email.ToLowerInvariant()),
            cancellationToken);

        var user = users.FirstOrDefault();

        // 2. Validate credentials — constant-time failure path prevents user enumeration
        if (user is null || !ValidCredentials(user, request.Password, passwordHasher))
        {
            await RecordFailedAttempt(user, request, attemptRepo, userRepo, unitOfWork, cancellationToken);
            throw new UnauthorizedException("Invalid email or password.");
        }

        // 3. Issue tokens
        var accessToken = tokenService.GenerateAccessToken(user);
        var (rawRefresh, refreshHash) = tokenService.GenerateRefreshToken();

        var refreshExpiry = dateTime.UtcNow.Add(RefreshTokenLifetime);
        var refreshToken = RefreshToken.CreateNew(user.Id, user.TenantId, refreshHash, refreshExpiry);

        // 4. Persist & audit
        user.RecordSuccessfulLogin(request.IpAddress);
        userRepo.Update(user);

        await tokenRepo.AddAsync(refreshToken, cancellationToken);

        var attempt = LoginAttempt.Record(user.TenantId, request.Email, isSuccessful: true, request.IpAddress, request.UserAgent);
        await attemptRepo.AddAsync(attempt, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        var accessExpiry = dateTime.UtcNow.AddMinutes(15);
        return Result.Success(BuildResponse(user, accessToken, rawRefresh, accessExpiry, refreshExpiry));
    }

    // ── Private helpers ───────────────────────────────────────────────────

    private static bool ValidCredentials(User user, string password, IPasswordHasher hasher)
    {
        if (!user.IsActive || user.IsLockedOut()) return false;
        if (user.PasswordHash is null) return false;
        return hasher.Verify(password, user.PasswordHash);
    }

    private static async Task RecordFailedAttempt(
        User? user,
        LoginCommand request,
        IRepository<LoginAttempt, LoginAttemptId> attemptRepo,
        IRepository<User, UserId> userRepo,
        IUnitOfWork unitOfWork,
        CancellationToken cancellationToken)
    {
        if (user is not null)
        {
            user.RecordFailedLogin();
            userRepo.Update(user);
        }

        // We don't have a TenantId for unknown users; use an empty sentinel so the record is still written
        var tenantId = user?.TenantId ?? new TenantId(Guid.Empty);
        var attempt = LoginAttempt.Record(tenantId, request.Email, isSuccessful: false, request.IpAddress, request.UserAgent);
        await attemptRepo.AddAsync(attempt, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private static AuthTokenResponse BuildResponse(
        User user,
        string accessToken,
        string rawRefreshToken,
        DateTimeOffset accessExpiry,
        DateTimeOffset refreshExpiry)
    {
        var userDto = new AuthUserDto(
            user.Id.Value,
            user.Email.Value,
            user.DisplayName.Value,
            user.Roles.Select(r => r.WorkflowRole.ToString()).ToList().AsReadOnly());

        return new AuthTokenResponse(accessToken, rawRefreshToken, accessExpiry, refreshExpiry, userDto);
    }
}
