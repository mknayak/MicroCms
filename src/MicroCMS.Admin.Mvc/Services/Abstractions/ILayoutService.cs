using MicroCMS.Admin.Mvc.Models.ApiDtos;

namespace MicroCMS.Admin.Mvc.Services.Abstractions;

public interface ILayoutService
{
    Task<List<LayoutListItem>> ListAsync(CancellationToken ct = default);
    Task<LayoutDto> GetByIdAsync(string id, CancellationToken ct = default);
    Task<LayoutDto> CreateAsync(CreateLayoutRequest request, CancellationToken ct = default);
    Task<LayoutDto> UpdateAsync(string id, UpdateLayoutRequest request, CancellationToken ct = default);
    Task DeleteAsync(string id, CancellationToken ct = default);
}
