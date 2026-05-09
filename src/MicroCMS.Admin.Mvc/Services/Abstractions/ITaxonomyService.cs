using MicroCMS.Admin.Mvc.Models.ApiDtos;

namespace MicroCMS.Admin.Mvc.Services.Abstractions;

public interface ITaxonomyService
{
    Task<List<CategoryDto>> ListCategoriesAsync(CancellationToken ct = default);
    Task<CategoryDto> CreateCategoryAsync(CreateCategoryRequest request, CancellationToken ct = default);
    Task DeleteCategoryAsync(string id, CancellationToken ct = default);
    Task<PagedResult<TagDto>> ListTagsAsync(int page = 1, int pageSize = 50, CancellationToken ct = default);
    Task<TagDto> CreateTagAsync(CreateTagRequest request, CancellationToken ct = default);
    Task DeleteTagAsync(string id, CancellationToken ct = default);
}
