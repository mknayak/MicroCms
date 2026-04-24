using MicroCMS.Application.Common.Attributes;
using MicroCMS.Application.Common.Authorization;
using MicroCMS.Application.Common.Markers;
using MicroCMS.Application.Features.Users.Dtos;

namespace MicroCMS.Application.Features.Users.Commands;

[HasPolicy(ContentPolicies.UserManage)]
public sealed record InviteUserCommand(
    string Email,
    string DisplayName) : ICommand<UserDto>;

[HasPolicy(ContentPolicies.UserManage)]
public sealed record AssignRoleCommand(
    Guid UserId,
    string WorkflowRole,
    Guid? SiteId = null) : ICommand<UserDto>;

[HasPolicy(ContentPolicies.UserManage)]
public sealed record RevokeRoleCommand(
    Guid UserId,
    Guid RoleId) : ICommand<UserDto>;

[HasPolicy(ContentPolicies.UserManage)]
public sealed record DeactivateUserCommand(Guid UserId) : ICommand<UserDto>;

[HasPolicy(ContentPolicies.UserManage)]
public sealed record ReactivateUserCommand(Guid UserId) : ICommand<UserDto>;
