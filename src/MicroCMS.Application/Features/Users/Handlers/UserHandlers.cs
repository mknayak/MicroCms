using MediatR;
using MicroCMS.Application.Common.Exceptions;
using MicroCMS.Application.Common.Interfaces;
using MicroCMS.Application.Features.Users.Commands;
using MicroCMS.Application.Features.Users.Dtos;
using MicroCMS.Application.Features.Users.Queries;
using MicroCMS.Domain.Aggregates.Identity;
using MicroCMS.Domain.Enums;
using MicroCMS.Domain.Repositories;
using MicroCMS.Domain.Specifications.Identity;
using MicroCMS.Domain.ValueObjects;
using MicroCMS.Shared.Ids;
using MicroCMS.Shared.Primitives;
using MicroCMS.Shared.Results;

namespace MicroCMS.Application.Features.Users.Handlers;

// ── Shared mapper ─────────────────────────────────────────────────────────────

internal static class UserMapper
{
    internal static UserDto ToDto(User u) => new(
        u.Id.Value,
        u.TenantId.Value,
      u.Email.Value,
        u.DisplayName.Value,
        u.IsActive,
     u.CreatedAt,
        u.UpdatedAt,
        u.Roles.Select(r => new UserRoleDto(r.Id.Value, r.WorkflowRole.ToString(), r.Name, r.SiteId?.Value)).ToList().AsReadOnly());

    internal static UserListItemDto ToListItemDto(User u) =>
      new(u.Id.Value, u.Email.Value, u.DisplayName.Value, u.IsActive, u.CreatedAt,
        u.LastLoginAt,
u.Roles.Select(r => r.Name).ToList().AsReadOnly());
}

// ── Query handlers ────────────────────────────────────────────────────────────

internal sealed class ListUsersQueryHandler(
    IRepository<User, UserId> repo)
    : IRequestHandler<ListUsersQuery, Result<PagedList<UserListItemDto>>>
{
    public async Task<Result<PagedList<UserListItemDto>>> Handle(ListUsersQuery request, CancellationToken cancellationToken)
    {
  var items = await repo.ListAsync(new AllUsersPagedSpec(request.Page, request.PageSize), cancellationToken);
        var total = await repo.CountAsync(new AllUsersCountSpec(), cancellationToken);
        return Result.Success(PagedList<UserListItemDto>.Create(
  items.Select(UserMapper.ToListItemDto), request.Page, request.PageSize, total));
    }
}

internal sealed class GetUserQueryHandler(
    IRepository<User, UserId> repo)
  : IRequestHandler<GetUserQuery, Result<UserDto>>
{
    public async Task<Result<UserDto>> Handle(GetUserQuery request, CancellationToken cancellationToken)
    {
        var user = await repo.GetByIdAsync(new UserId(request.UserId), cancellationToken)
 ?? throw new NotFoundException(nameof(User), request.UserId);
return Result.Success(UserMapper.ToDto(user));
    }
}

// ── Command handlers ──────────────────────────────────────────────────────────

internal sealed class InviteUserCommandHandler(
    IRepository<User, UserId> repo,
    ICurrentUser currentUser)
    : IRequestHandler<InviteUserCommand, Result<UserDto>>
{
    public async Task<Result<UserDto>> Handle(InviteUserCommand request, CancellationToken cancellationToken)
    {
        var email = EmailAddress.Create(request.Email);

      // Uniqueness check within tenant (query filter already scopes to current tenant)
        var existing = await repo.ListAsync(new UserByEmailSpec(email.Value), cancellationToken);
  if (existing.Count > 0)
     throw new ConflictException(nameof(User), request.Email);

 var user = User.Create(currentUser.TenantId, email, PersonName.Create(request.DisplayName));
        await repo.AddAsync(user, cancellationToken);
    return Result.Success(UserMapper.ToDto(user));
    }
}

internal sealed class AssignRoleCommandHandler(
    IRepository<User, UserId> repo)
    : IRequestHandler<AssignRoleCommand, Result<UserDto>>
{
    public async Task<Result<UserDto>> Handle(AssignRoleCommand request, CancellationToken cancellationToken)
    {
        var user = await repo.GetByIdAsync(new UserId(request.UserId), cancellationToken)
            ?? throw new NotFoundException(nameof(User), request.UserId);

        if (!Enum.TryParse<WorkflowRole>(request.WorkflowRole, ignoreCase: true, out var role))
        throw new ValidationException([new FluentValidation.Results.ValidationFailure(
            "WorkflowRole", $"'{request.WorkflowRole}' is not a valid WorkflowRole.")]);

        var siteId = request.SiteId.HasValue ? new SiteId(request.SiteId.Value) : (SiteId?)null;
        user.AssignRole(role, request.WorkflowRole, siteId);
        repo.Update(user);
      return Result.Success(UserMapper.ToDto(user));
    }
}

internal sealed class RevokeRoleCommandHandler(
    IRepository<User, UserId> repo)
    : IRequestHandler<RevokeRoleCommand, Result<UserDto>>
{
    public async Task<Result<UserDto>> Handle(RevokeRoleCommand request, CancellationToken cancellationToken)
    {
        var user = await repo.GetByIdAsync(new UserId(request.UserId), cancellationToken)
            ?? throw new NotFoundException(nameof(User), request.UserId);

        user.RevokeRole(new RoleId(request.RoleId));
        repo.Update(user);
        return Result.Success(UserMapper.ToDto(user));
    }
}

internal sealed class DeactivateUserCommandHandler(
    IRepository<User, UserId> repo)
    : IRequestHandler<DeactivateUserCommand, Result<UserDto>>
{
    public async Task<Result<UserDto>> Handle(DeactivateUserCommand request, CancellationToken cancellationToken)
    {
        var user = await repo.GetByIdAsync(new UserId(request.UserId), cancellationToken)
   ?? throw new NotFoundException(nameof(User), request.UserId);

    user.Deactivate();
  repo.Update(user);
return Result.Success(UserMapper.ToDto(user));
    }
}

internal sealed class ReactivateUserCommandHandler(
    IRepository<User, UserId> repo)
    : IRequestHandler<ReactivateUserCommand, Result<UserDto>>
{
    public async Task<Result<UserDto>> Handle(ReactivateUserCommand request, CancellationToken cancellationToken)
    {
        var user = await repo.GetByIdAsync(new UserId(request.UserId), cancellationToken)
          ?? throw new NotFoundException(nameof(User), request.UserId);

  user.Reactivate();
        repo.Update(user);
        return Result.Success(UserMapper.ToDto(user));
    }
}
