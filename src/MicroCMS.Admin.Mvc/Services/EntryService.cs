using MicroCMS.Admin.Mvc.Infrastructure;
using MicroCMS.Admin.Mvc.Models.ApiDtos;
using MicroCMS.Admin.Mvc.Services.Abstractions;

namespace MicroCMS.Admin.Mvc.Services;

public sealed class EntryService : ApiClientBase, IEntryService
{
    public EntryService(IHttpClientFactory httpClientFactory) : base(httpClientFactory) { }

    public Task<PagedResult<EntryListItem>> ListAsync(EntryListParams parameters, CancellationToken ct = default)
    {
        var qs = BuildQueryString(parameters);
        return GetAsync<PagedResult<EntryListItem>>($"entries?{qs}", ct);
    }

    public Task<EntryDto> GetByIdAsync(string id, CancellationToken ct = default) =>
        GetAsync<EntryDto>($"entries/{id}", ct);

    public Task<EntryDto> CreateAsync(CreateEntryRequest request, CancellationToken ct = default) =>
        PostAsync<EntryDto>("entries", request, ct);

    public Task<EntryDto> UpdateAsync(string id, UpdateEntryRequest request, CancellationToken ct = default) =>
        PutAsync<EntryDto>($"entries/{id}", request, ct);

    public Task<EntryDto> PublishAsync(string id, CancellationToken ct = default) =>
        PostAsync<EntryDto>($"entries/{id}/publish", ct: ct);

    public Task<EntryDto> UnpublishAsync(string id, CancellationToken ct = default) =>
        PostAsync<EntryDto>($"entries/{id}/unpublish", ct: ct);

    public Task DeleteAsync(string id, CancellationToken ct = default) =>
        DeleteAsync($"entries/{id}", ct);

    private static string BuildQueryString(EntryListParams p)
    {
        var parts = new List<string>
        {
            $"pageNumber={p.PageNumber}",
            $"pageSize={p.PageSize}",
        };

        if (!string.IsNullOrWhiteSpace(p.ContentTypeId)) parts.Add($"contentTypeId={Uri.EscapeDataString(p.ContentTypeId)}");
        if (!string.IsNullOrWhiteSpace(p.SiteId)) parts.Add($"siteId={Uri.EscapeDataString(p.SiteId)}");
        if (!string.IsNullOrWhiteSpace(p.Status)) parts.Add($"status={Uri.EscapeDataString(p.Status)}");
        if (!string.IsNullOrWhiteSpace(p.Locale)) parts.Add($"locale={Uri.EscapeDataString(p.Locale)}");
        if (!string.IsNullOrWhiteSpace(p.Search)) parts.Add($"search={Uri.EscapeDataString(p.Search)}");

        return string.Join("&", parts);
    }
}
