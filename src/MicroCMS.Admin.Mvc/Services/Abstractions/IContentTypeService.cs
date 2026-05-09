using MicroCMS.Admin.Mvc.Models.ApiDtos;

namespace MicroCMS.Admin.Mvc.Services.Abstractions;

public interface IContentTypeService
{
    Task<PagedResult<ContentTypeListItem>> ListAsync(int page = 1, int pageSize = 20, CancellationToken ct = default);
    Task<ContentTypeDto> GetByIdAsync(string id, CancellationToken ct = default);
    Task<ContentTypeDto> CreateAsync(CreateContentTypeRequest request, CancellationToken ct = default);
    Task<ContentTypeDto> UpdateAsync(string id, UpdateContentTypeRequest request, CancellationToken ct = default);
    Task DeleteAsync(string id, CancellationToken ct = default);

    // ── Field management ─────────────────────────────────────────────────────
    Task<ContentTypeDto> AddFieldAsync(string contentTypeId, AddFieldRequest request, CancellationToken ct = default);
    Task<ContentTypeDto> UpdateFieldAsync(string contentTypeId, string fieldId, UpdateFieldRequest request, CancellationToken ct = default);
    Task DeleteFieldAsync(string contentTypeId, string fieldId, CancellationToken ct = default);
}
