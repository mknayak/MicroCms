using MicroCMS.Admin.Mvc.Models.ApiDtos;

namespace MicroCMS.Admin.Mvc.Services.Abstractions;

public interface IComponentService
{
    Task<PagedResult<ComponentListItem>> ListAsync(int page = 1, int pageSize = 20, CancellationToken ct = default);
    Task<ComponentDto> GetByIdAsync(string id, CancellationToken ct = default);
    Task<ComponentDto> CreateAsync(CreateComponentRequest request, CancellationToken ct = default);
    Task DeleteAsync(string id, CancellationToken ct = default);
    Task<PagedResult<ComponentItemDto>> ListItemsAsync(string componentId, int page = 1, int pageSize = 20, CancellationToken ct = default);
}
