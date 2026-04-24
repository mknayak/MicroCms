using MicroCMS.Application.Common.Attributes;
using MicroCMS.Application.Common.Authorization;
using MicroCMS.Application.Common.Markers;
using MicroCMS.Application.Features.Tenants.Dtos;
using MicroCMS.Shared.Primitives;

namespace MicroCMS.Application.Features.Tenants.Queries;

[HasPolicy(ContentPolicies.TenantManage)]
public sealed record GetTenantQuery(Guid TenantId) : IQuery<TenantDto>;

/// <summary>Returns the tenant of the currently authenticated user.</summary>
[HasPolicy(ContentPolicies.TenantManage)]
public sealed record GetCurrentTenantQuery : IQuery<TenantDto>;

[HasPolicy(ContentPolicies.SystemAdmin)]
public sealed record ListTenantsQuery(int Page = 1, int PageSize = 20) : IQuery<PagedList<TenantListItemDto>>;
