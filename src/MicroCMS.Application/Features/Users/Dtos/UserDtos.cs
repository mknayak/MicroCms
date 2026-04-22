namespace MicroCMS.Application.Features.Users.Dtos;

public sealed record UserDto(
    Guid Id,
    Guid TenantId,
    string Email,
 string DisplayName,
    bool IsActive,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    IReadOnlyList<UserRoleDto> Roles);

public sealed record UserRoleDto(
    Guid Id,
    string WorkflowRole,
    string RoleName,
    Guid? SiteId);

public sealed record UserListItemDto(
    Guid Id,
    string Email,
    string DisplayName,
    bool IsActive,
    DateTimeOffset CreatedAt);
