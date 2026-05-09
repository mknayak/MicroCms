using MicroCMS.Admin.Mvc.Models.ApiDtos;

namespace MicroCMS.Admin.Mvc.Services.Abstractions;

public interface IEntryService
{
    Task<PagedResult<EntryListItem>> ListAsync(EntryListParams parameters, CancellationToken ct = default);
    Task<EntryDto> GetByIdAsync(string id, CancellationToken ct = default);
    Task<EntryDto> CreateAsync(CreateEntryRequest request, CancellationToken ct = default);
    Task<EntryDto> UpdateAsync(string id, UpdateEntryRequest request, CancellationToken ct = default);
    Task<EntryDto> PublishAsync(string id, CancellationToken ct = default);
    Task<EntryDto> UnpublishAsync(string id, CancellationToken ct = default);
    Task DeleteAsync(string id, CancellationToken ct = default);
}
