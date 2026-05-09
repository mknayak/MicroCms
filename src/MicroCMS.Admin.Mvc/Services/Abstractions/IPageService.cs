using MicroCMS.Admin.Mvc.Models.ApiDtos;

namespace MicroCMS.Admin.Mvc.Services.Abstractions;

public interface IPageService
{
    Task<List<PageTreeNode>> GetTreeAsync(CancellationToken ct = default);
    Task<PageDto> GetByIdAsync(string id, CancellationToken ct = default);
    Task<PageDto> CreateStaticPageAsync(CreateStaticPageRequest request, CancellationToken ct = default);
    Task DeleteAsync(string id, CancellationToken ct = default);
}
