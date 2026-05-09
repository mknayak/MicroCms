namespace MicroCMS.Admin.Mvc.Models.ApiDtos;

public sealed class UserDto
{
    public string Id { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public List<string> Roles { get; init; } = [];
    public bool IsActive { get; init; }
    public string? AvatarUrl { get; init; }
    public string? LastLoginAt { get; init; }
    public string CreatedAt { get; init; } = string.Empty;
}

public sealed class InviteUserRequest
{
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
}

public sealed class UpdateUserRolesRequest
{
    public List<string> Roles { get; set; } = [];
}
