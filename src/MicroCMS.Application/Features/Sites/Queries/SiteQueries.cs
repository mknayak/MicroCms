using MediatR;
using MediatR;
using MicroCMS.Application.Common.Attributes;
using MicroCMS.Application.Common.Authorization;
using MicroCMS.Application.Common.Exceptions;
using MicroCMS.Application.Common.Markers;
using MicroCMS.Application.Features.Sites.Commands;
using MicroCMS.Domain.Aggregates.Tenant;
using MicroCMS.Domain.Repositories;
using MicroCMS.Domain.Specifications.Tenant;
using MicroCMS.Shared.Ids;
using MicroCMS.Shared.Results;

namespace MicroCMS.Application.Features.Sites.Queries;

// ── DTOs ──────────────────────────────────────────────────────────────────────

public sealed record SiteDetailDto(
    Guid Id,
    Guid TenantId,
    string Name,
    string Handle,
    string DefaultLocale,
    bool IsActive,
    string? CustomDomain,
    DateTimeOffset CreatedAt,
    IReadOnlyList<SiteEnvironmentDto> Environments);

// ── Queries ───────────────────────────────────────────────────────────────────

/// <summary>Returns the full detail of a single site, including its deployment environments.</summary>
[HasPolicy(ContentPolicies.TenantManage)]
public sealed record GetSiteQuery(Guid SiteId) : IQuery<SiteDetailDto>;

/// <summary>Returns the per-site feature flags, preview URL, CORS origins, and locales.</summary>
[HasPolicy(ContentPolicies.TenantManage)]
public sealed record GetSiteSettingsQuery(Guid SiteId) : IQuery<SiteSettingsDto>;

// ── Handlers ──────────────────────────────────────────────────────────────────

internal sealed class GetSiteQueryHandler(
    IRepository<Domain.Aggregates.Tenant.Tenant, TenantId> tenantRepo)
    : IRequestHandler<GetSiteQuery, Result<SiteDetailDto>>
{
    public async Task<Result<SiteDetailDto>> Handle(
        GetSiteQuery request, CancellationToken cancellationToken)
    {
        var siteId = new SiteId(request.SiteId);
        var tenants = await tenantRepo.ListAsync(new TenantBySiteIdSpec(siteId), cancellationToken);
        var tenant = tenants.FirstOrDefault()
            ?? throw new NotFoundException(nameof(Site), request.SiteId);

        var site = tenant.Sites.First(s => s.Id == siteId);
        return Result.Success(SiteDetailMapper.ToDto(site));
    }
}

internal sealed class GetSiteSettingsQueryHandler(
    IRepository<SiteSettings, SiteId> settingsRepo,
    IRepository<Domain.Aggregates.Tenant.Tenant, TenantId> tenantRepo)
    : IRequestHandler<GetSiteSettingsQuery, Result<SiteSettingsDto>>
{
    public async Task<Result<SiteSettingsDto>> Handle(
        GetSiteSettingsQuery request, CancellationToken cancellationToken)
    {
        // Verify the site exists (Site is owned by Tenant — must query via aggregate root).
        var siteId = new SiteId(request.SiteId);
        var tenants = await tenantRepo.ListAsync(new TenantBySiteIdSpec(siteId), cancellationToken);
        if (!tenants.Any())
            throw new NotFoundException(nameof(Site), request.SiteId);

        var settings = await settingsRepo.GetByIdAsync(siteId, cancellationToken);

        // Return defaults when no settings row exists yet.
        if (settings is null)
        {
            return Result.Success(new SiteSettingsDto(
                request.SiteId,
                PreviewUrlTemplate: null,
                VersioningEnabled: true,
                WorkflowEnabled: true,
                SchedulingEnabled: true,
                PreviewEnabled: true,
                AiEnabled: true,
                CorsOrigins: [],
                Locales: ["en"]));
        }

        return Result.Success(SiteSettingsMapper.ToDto(settings));
    }
}

// ── Mappers ───────────────────────────────────────────────────────────────────

internal static class SiteDetailMapper
{
    internal static SiteDetailDto ToDto(Site s) => new(
        s.Id.Value,
        s.TenantId.Value,
        s.Name,
        s.Handle.Value,
        s.DefaultLocale.Value,
        s.IsActive,
        s.CustomDomain?.Value,
        s.CreatedAt,
        s.Environments.Select(e => new SiteEnvironmentDto(
            e.Type.ToString(), e.Url, e.SslStatus.ToString(), e.IsLive)).ToList());
}
