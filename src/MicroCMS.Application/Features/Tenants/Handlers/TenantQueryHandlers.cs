using MediatR;
using MicroCMS.Application.Common.Exceptions;
using MicroCMS.Application.Common.Interfaces;
using MicroCMS.Application.Features.Tenants.Dtos;
using MicroCMS.Application.Features.Tenants.Mappers;
using MicroCMS.Application.Features.Tenants.Queries;
using MicroCMS.Domain.Aggregates.Tenant;
using MicroCMS.Domain.Repositories;
using MicroCMS.Domain.Specifications.Tenant;
using MicroCMS.Shared.Ids;
using MicroCMS.Shared.Primitives;
using MicroCMS.Shared.Results;

namespace MicroCMS.Application.Features.Tenants.Handlers;

internal sealed class GetTenantQueryHandler(
    IRepository<Domain.Aggregates.Tenant.Tenant, TenantId> tenantRepo)
    : IRequestHandler<GetTenantQuery, Result<TenantDto>>
{
    public async Task<Result<TenantDto>> Handle(GetTenantQuery request, CancellationToken cancellationToken)
    {
        var tenant = await tenantRepo.GetByIdAsync(new TenantId(request.TenantId), cancellationToken)
    ?? throw new NotFoundException(nameof(Tenant), request.TenantId);
        return Result.Success(TenantMapper.ToDto(tenant));
    }
}

internal sealed class ListTenantsQueryHandler(
    IRepository<Domain.Aggregates.Tenant.Tenant, TenantId> tenantRepo)
 : IRequestHandler<ListTenantsQuery, Result<PagedList<TenantListItemDto>>>
{
    public async Task<Result<PagedList<TenantListItemDto>>> Handle(ListTenantsQuery request, CancellationToken cancellationToken)
    {
       var spec = new AllTenantsSpec(request.Page, request.PageSize);
      var countSpec = new AllTenantsCountSpec();

        var items = await tenantRepo.ListAsync(spec, cancellationToken);
  var total = await tenantRepo.CountAsync(countSpec, cancellationToken);

        return Result.Success(PagedList<TenantListItemDto>.Create(
   items.Select(TenantMapper.ToListItemDto),
     request.Page, request.PageSize, total));
  }
}

internal sealed class GetCurrentTenantQueryHandler(
    IRepository<Domain.Aggregates.Tenant.Tenant, TenantId> tenantRepo,
    ICurrentUser currentUser)
  : IRequestHandler<GetCurrentTenantQuery, Result<TenantDto>>
{
    public async Task<Result<TenantDto>> Handle(GetCurrentTenantQuery request, CancellationToken cancellationToken)
    {
        var tenant = await tenantRepo.GetByIdAsync(currentUser.TenantId, cancellationToken)
 ?? throw new NotFoundException(nameof(Tenant), currentUser.TenantId);
        return Result.Success(TenantMapper.ToDto(tenant));
    }
}
