using MediatR;
using MicroCMS.Application.Common.Exceptions;
using MicroCMS.Application.Features.Tenants.Commands;
using MicroCMS.Application.Features.Tenants.Dtos;
using MicroCMS.Application.Features.Tenants.Mappers;
using MicroCMS.Domain.Aggregates.Tenant;
using MicroCMS.Domain.Repositories;
using MicroCMS.Domain.Specifications.Tenant;
using MicroCMS.Domain.ValueObjects;
using MicroCMS.Shared.Ids;
using MicroCMS.Shared.Results;

namespace MicroCMS.Application.Features.Tenants.Handlers;

internal sealed class CreateTenantCommandHandler(
    IRepository<Domain.Aggregates.Tenant.Tenant, TenantId> tenantRepo)
    : IRequestHandler<CreateTenantCommand, Result<TenantDto>>
{
    public async Task<Result<TenantDto>> Handle(CreateTenantCommand request, CancellationToken cancellationToken)
    {
        var slug = TenantSlug.Create(request.Slug);

        // Uniqueness check
        var existing = await tenantRepo.ListAsync(new TenantBySlugSpec(slug.Value), cancellationToken);
        if (existing.Count > 0)
            throw new ConflictException("Tenant", request.Slug);

        var locale = Locale.Create(request.DefaultLocale);
        var settings = TenantSettings.Create(request.DisplayName, locale, timeZoneId: request.TimeZoneId, aiEnabled: request.AiEnabled);
        var tenant = Domain.Aggregates.Tenant.Tenant.Create(slug, settings);

        await tenantRepo.AddAsync(tenant, cancellationToken);
        return Result.Success(TenantMapper.ToDto(tenant));
    }
}

internal sealed class UpdateTenantSettingsCommandHandler(
    IRepository<Domain.Aggregates.Tenant.Tenant, TenantId> tenantRepo)
    : IRequestHandler<UpdateTenantSettingsCommand, Result<TenantDto>>
{
    public async Task<Result<TenantDto>> Handle(UpdateTenantSettingsCommand request, CancellationToken cancellationToken)
    {
        var tenant = await tenantRepo.GetByIdAsync(new TenantId(request.TenantId), cancellationToken)
                ?? throw new NotFoundException(nameof(Tenant), request.TenantId);

        var newSettings = TenantSettings.Create(
                request.DisplayName,
                Locale.Create(request.DefaultLocale),
                timeZoneId: request.TimeZoneId,
                aiEnabled: request.AiEnabled,
                logoUrl: request.LogoUrl);

        tenant.UpdateSettings(newSettings);
        tenantRepo.Update(tenant);
        return Result.Success(TenantMapper.ToDto(tenant));
    }
}

internal sealed class AddSiteCommandHandler(
    IRepository<Domain.Aggregates.Tenant.Tenant, TenantId> tenantRepo)
    : IRequestHandler<AddSiteCommand, Result<SiteDto>>
{
    public async Task<Result<SiteDto>> Handle(AddSiteCommand request, CancellationToken cancellationToken)
    {
        var tenant = await tenantRepo.GetByIdAsync(new TenantId(request.TenantId), cancellationToken)
                ?? throw new NotFoundException(nameof(Tenant), request.TenantId);

        var site = tenant.AddSite(
                request.Name,
                Slug.Create(request.Handle),
                Locale.Create(request.DefaultLocale));

        tenantRepo.Update(tenant);
        return Result.Success(TenantMapper.ToSiteDto(site));
    }
}
