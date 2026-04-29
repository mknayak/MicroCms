using MediatR;
using MicroCMS.Application.Common.Attributes;
using MicroCMS.Application.Common.Authorization;
using MicroCMS.Application.Common.Exceptions;
using MicroCMS.Application.Common.Markers;
using MicroCMS.Application.Features.Sites.Queries;
using MicroCMS.Domain.Aggregates.Tenant;
using MicroCMS.Domain.Repositories;
using MicroCMS.Domain.Specifications.Tenant;
using MicroCMS.Domain.ValueObjects;
using MicroCMS.Shared.Ids;
using MicroCMS.Shared.Results;

namespace MicroCMS.Application.Features.Sites.Commands;

// ── Command ───────────────────────────────────────────────────────────────────

/// <summary>
/// Updates the mutable properties of a site: display name, default locale,
/// and custom domain. The handle is immutable after creation.
/// </summary>
[HasPolicy(ContentPolicies.TenantManage)]
public sealed record UpdateSiteCommand(
    Guid SiteId,
    string Name,
    string DefaultLocale,
    string? CustomDomain) : ICommand<SiteDetailDto>;

// ── Handler ───────────────────────────────────────────────────────────────────

internal sealed class UpdateSiteCommandHandler(
    IRepository<Domain.Aggregates.Tenant.Tenant, TenantId> tenantRepo)
    : IRequestHandler<UpdateSiteCommand, Result<SiteDetailDto>>
{
    public async Task<Result<SiteDetailDto>> Handle(
        UpdateSiteCommand request, CancellationToken cancellationToken)
    {
        var siteId = new SiteId(request.SiteId);
        var tenants = await tenantRepo.ListAsync(new TenantBySiteIdSpec(siteId), cancellationToken);
        var tenant = tenants.FirstOrDefault()
            ?? throw new NotFoundException(nameof(Site), request.SiteId);

        var site = tenant.Sites.First(s => s.Id == siteId);

        site.Rename(request.Name);
        site.SetDefaultLocale(Locale.Create(request.DefaultLocale));

        if (!string.IsNullOrWhiteSpace(request.CustomDomain))
            site.SetCustomDomain(CustomDomain.Create(request.CustomDomain));
        else
            site.SetCustomDomain(null);

        tenantRepo.Update(tenant);

        return Result.Success(SiteDetailMapper.ToDto(site));
    }
}
