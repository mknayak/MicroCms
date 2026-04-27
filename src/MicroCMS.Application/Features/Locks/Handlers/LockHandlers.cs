using MediatR;
using MicroCMS.Application.Common.Exceptions;
using MicroCMS.Application.Common.Interfaces;
using MicroCMS.Application.Features.Locks.Commands;
using MicroCMS.Domain.Aggregates.Locks;
using MicroCMS.Domain.Repositories;
using MicroCMS.Domain.Specifications.Locks;
using MicroCMS.Shared.Ids;
using MicroCMS.Shared.Results;

namespace MicroCMS.Application.Features.Locks.Handlers;

internal static class LockMapper
{
    internal static EditLockDto ToDto(EditLock l) => new(
        l.EntityId, l.EntityType, l.LockedByUserId,
     l.LockedByDisplayName, l.LockedAt, l.ExpiresAt);
}

internal sealed class AcquireLockCommandHandler(
    IRepository<EditLock, EditLockId> repo,
  ICurrentUser currentUser,
    IUnitOfWork unitOfWork)
    : IRequestHandler<AcquireLockCommand, Result<EditLockDto>>
{
    public async Task<Result<EditLockDto>> Handle(AcquireLockCommand request, CancellationToken cancellationToken)
  {
        var existing = (await repo.ListAsync(new LockByEntityIdSpec(request.EntityId), cancellationToken))
    .FirstOrDefault();

        if (existing is not null)
        {
       if (!existing.IsExpired && !existing.IsOwnedBy(currentUser.UserId))
           throw new ConflictException("EditLock", $"locked by {existing.LockedByDisplayName} until {existing.ExpiresAt:HH:mm}");
  repo.Remove(existing);
        }

        var displayName = currentUser.Email ?? currentUser.UserId.ToString();
        var newLock = EditLock.Acquire(request.EntityId, request.EntityType, currentUser.UserId, displayName);
      await repo.AddAsync(newLock, cancellationToken);
      await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success(LockMapper.ToDto(newLock));
    }
}

internal sealed class ReleaseLockCommandHandler(
    IRepository<EditLock, EditLockId> repo,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork)
    : IRequestHandler<ReleaseLockCommand, Result>
{
    public async Task<Result> Handle(ReleaseLockCommand request, CancellationToken cancellationToken)
    {
 var existing = (await repo.ListAsync(new LockByEntityIdSpec(request.EntityId), cancellationToken))
  .FirstOrDefault();
     if (existing is null) return Result.Success();
      if (!existing.IsOwnedBy(currentUser.UserId) && !currentUser.Roles.Contains("SystemAdmin"))
   throw new ForbiddenException("You do not own this lock.");
        repo.Remove(existing);
     await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

internal sealed class RefreshLockCommandHandler(
    IRepository<EditLock, EditLockId> repo,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork)
    : IRequestHandler<RefreshLockCommand, Result<EditLockDto>>
{
    public async Task<Result<EditLockDto>> Handle(RefreshLockCommand request, CancellationToken cancellationToken)
    {
        var existing = (await repo.ListAsync(new LockByEntityIdSpec(request.EntityId), cancellationToken))
            .FirstOrDefault()
    ?? throw new NotFoundException(nameof(EditLock), request.EntityId);
        if (!existing.IsOwnedBy(currentUser.UserId))
  throw new ForbiddenException("You do not own this lock.");
        existing.Refresh();
        repo.Update(existing);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success(LockMapper.ToDto(existing));
    }
}

internal sealed class GetLockQueryHandler(IRepository<EditLock, EditLockId> repo)
    : IRequestHandler<GetLockQuery, Result<EditLockDto?>>
{
    public async Task<Result<EditLockDto?>> Handle(GetLockQuery request, CancellationToken cancellationToken)
    {
        var existing = (await repo.ListAsync(new LockByEntityIdSpec(request.EntityId), cancellationToken))
      .FirstOrDefault();
 if (existing is null || existing.IsExpired) return Result.Success<EditLockDto?>(null);
      return Result.Success<EditLockDto?>(LockMapper.ToDto(existing));
    }
}
