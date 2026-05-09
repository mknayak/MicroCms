using MicroCMS.Admin.Mvc.Infrastructure;
using MicroCMS.Admin.Mvc.Models.ApiDtos;
using MicroCMS.Admin.Mvc.Services.Abstractions;

namespace MicroCMS.Admin.Mvc.Services;

public sealed class ContentTypeService : ApiClientBase, IContentTypeService
{
    public ContentTypeService(IHttpClientFactory httpClientFactory) : base(httpClientFactory) { }

    public Task<PagedResult<ContentTypeListItem>> ListAsync(int page = 1, int pageSize = 20, CancellationToken ct = default) =>
        GetAsync<PagedResult<ContentTypeListItem>>($"content-types?pageNumber={page}&pageSize={pageSize}", ct);

    public Task<ContentTypeDto> GetByIdAsync(string id, CancellationToken ct = default) =>
        GetAsync<ContentTypeDto>($"content-types/{id}", ct);

    public Task<ContentTypeDto> CreateAsync(CreateContentTypeRequest request, CancellationToken ct = default) =>
        PostAsync<ContentTypeDto>("content-types", request, ct);

    public Task<ContentTypeDto> UpdateAsync(string id, UpdateContentTypeRequest request, CancellationToken ct = default) =>
        PutAsync<ContentTypeDto>($"content-types/{id}", request, ct);

    public Task DeleteAsync(string id, CancellationToken ct = default) =>
        DeleteAsync($"content-types/{id}", ct);

    public Task<ContentTypeDto> AddFieldAsync(string contentTypeId, AddFieldRequest request, CancellationToken ct = default) =>
        PostAsync<ContentTypeDto>($"content-types/{contentTypeId}/fields", request, ct);

    public Task<ContentTypeDto> UpdateFieldAsync(string contentTypeId, string fieldId, UpdateFieldRequest request, CancellationToken ct = default) =>
        PutAsync<ContentTypeDto>($"content-types/{contentTypeId}/fields/{fieldId}", request, ct);

    public Task DeleteFieldAsync(string contentTypeId, string fieldId, CancellationToken ct = default) =>
        DeleteAsync($"content-types/{contentTypeId}/fields/{fieldId}", ct);
}
