using MicroCMS.Admin.Mvc.Infrastructure;
using MicroCMS.Admin.Mvc.Models.ApiDtos;
using MicroCMS.Admin.Mvc.Services.Abstractions;

namespace MicroCMS.Admin.Mvc.Services;

public sealed class UserService : ApiClientBase, IUserService
{
    public UserService(IHttpClientFactory httpClientFactory) : base(httpClientFactory) { }

    public Task<PagedResult<UserDto>> ListAsync(int page = 1, int pageSize = 20, CancellationToken ct = default) =>
        GetAsync<PagedResult<UserDto>>($"admin/users?pageNumber={page}&pageSize={pageSize}", ct);

    public Task<UserDto> GetByIdAsync(string id, CancellationToken ct = default) =>
        GetAsync<UserDto>($"admin/users/{id}", ct);

    public Task<UserDto> InviteAsync(InviteUserRequest request, CancellationToken ct = default) =>
        PostAsync<UserDto>("admin/users/invite", request, ct);

    public Task UpdateRolesAsync(string id, UpdateUserRolesRequest request, CancellationToken ct = default) =>
        PutAsync($"admin/users/{id}/roles", request, ct);

    public Task DeactivateAsync(string id, CancellationToken ct = default) =>
        PostAsync($"admin/users/{id}/deactivate", ct: ct);

    public Task ActivateAsync(string id, CancellationToken ct = default) =>
        PostAsync($"admin/users/{id}/activate", ct: ct);

    public Task DeleteAsync(string id, CancellationToken ct = default) =>
        DeleteAsync($"admin/users/{id}", ct);
}
