using MicroCMS.Admin.Mvc.Models.ApiDtos;

namespace MicroCMS.Admin.Mvc.Services.Abstractions;

public interface IUserService
{
    Task<PagedResult<UserDto>> ListAsync(int page = 1, int pageSize = 20, CancellationToken ct = default);
    Task<UserDto> GetByIdAsync(string id, CancellationToken ct = default);
    Task<UserDto> InviteAsync(InviteUserRequest request, CancellationToken ct = default);
    Task UpdateRolesAsync(string id, UpdateUserRolesRequest request, CancellationToken ct = default);
    Task DeactivateAsync(string id, CancellationToken ct = default);
    Task ActivateAsync(string id, CancellationToken ct = default);
    Task DeleteAsync(string id, CancellationToken ct = default);
}
