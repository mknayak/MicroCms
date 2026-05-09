using MicroCMS.Admin.Mvc.Models.ApiDtos;

namespace MicroCMS.Admin.Mvc.Models.ViewModels.Tenants;

public sealed class TenantsViewModel
{
    public List<TenantListItem> Items { get; init; } = [];
    public int TotalCount { get; init; }
    public int PageNumber { get; init; } = 1;
    public int TotalPages { get; init; }
}

public sealed class TenantDetailViewModel
{
    public TenantDetail Tenant { get; init; } = new();
}
