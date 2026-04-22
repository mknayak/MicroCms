using MicroCMS.Application.Common.Attributes;
using MicroCMS.Application.Common.Authorization;
using MicroCMS.Application.Common.Markers;
using MicroCMS.Application.Features.Users.Dtos;
using MicroCMS.Shared.Primitives;

namespace MicroCMS.Application.Features.Users.Queries;

[HasPolicy(ContentPolicies.UserManage)]
public sealed record ListUsersQuery(
    int Page = 1,
    int PageSize = 20) : IQuery<PagedList<UserListItemDto>>;

[HasPolicy(ContentPolicies.UserManage)]
public sealed record GetUserQuery(Guid UserId) : IQuery<UserDto>;
