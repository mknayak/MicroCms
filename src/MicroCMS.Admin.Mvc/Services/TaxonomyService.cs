using MicroCMS.Admin.Mvc.Infrastructure;
using MicroCMS.Admin.Mvc.Models.ApiDtos;
using MicroCMS.Admin.Mvc.Services.Abstractions;

namespace MicroCMS.Admin.Mvc.Services;

public sealed class TaxonomyService : ApiClientBase, ITaxonomyService
{
    public TaxonomyService(IHttpClientFactory httpClientFactory) : base(httpClientFactory) { }

    public Task<List<CategoryDto>> ListCategoriesAsync(CancellationToken ct = default) =>
        GetAsync<List<CategoryDto>>("taxonomy/categories", ct);

    public Task<CategoryDto> CreateCategoryAsync(CreateCategoryRequest request, CancellationToken ct = default) =>
        PostAsync<CategoryDto>("taxonomy/categories", request, ct);

    public Task DeleteCategoryAsync(string id, CancellationToken ct = default) =>
        DeleteAsync($"taxonomy/categories/{id}", ct);

    public Task<PagedResult<TagDto>> ListTagsAsync(int page = 1, int pageSize = 50, CancellationToken ct = default) =>
        GetAsync<PagedResult<TagDto>>($"taxonomy/tags?pageNumber={page}&pageSize={pageSize}", ct);

    public Task<TagDto> CreateTagAsync(CreateTagRequest request, CancellationToken ct = default) =>
        PostAsync<TagDto>("taxonomy/tags", request, ct);

    public Task DeleteTagAsync(string id, CancellationToken ct = default) =>
        DeleteAsync($"taxonomy/tags/{id}", ct);
}
