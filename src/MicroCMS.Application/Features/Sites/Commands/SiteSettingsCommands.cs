using MediatR;
using MicroCMS.Application.Common.Attributes;
using MicroCMS.Application.Common.Authorization;
using MicroCMS.Application.Common.Exceptions;
using MicroCMS.Application.Common.Interfaces;
using MicroCMS.Application.Common.Markers;
using MicroCMS.Domain.Aggregates.Tenant;
using MicroCMS.Domain.Enums;
using MicroCMS.Domain.Repositories;
using MicroCMS.Domain.ValueObjects;
using MicroCMS.Shared.Ids;
using MicroCMS.Shared.Results;

namespace MicroCMS.Application.Features.Sites.Commands;

// ── DTOs ──────────────────────────────────────────────────────────────────────

public sealed record SiteEnvironmentDto(string Type, string Url, string SslStatus, bool IsLive);

public sealed record SiteSettingsDto(
    Guid SiteId,
    string? PreviewUrlTemplate,
    bool VersioningEnabled,
    bool WorkflowEnabled,
    bool SchedulingEnabled,
    bool PreviewEnabled,
    bool AiEnabled,
    IReadOnlyList<string> CorsOrigins,
    IReadOnlyList<string> Locales);

// ── Commands ──────────────────────────────────────────────────────────────────

/// <summary>Adds or updates a deployment environment on a site (GAP-17).</summary>
[HasPolicy(ContentPolicies.TenantManage)]
public sealed record UpsertSiteEnvironmentCommand(
    Guid SiteId,
    string EnvironmentType,
    string Url,
    bool IsLive = false) : ICommand<SiteEnvironmentDto>;

/// <summary>Updates per-site feature flags, preview URL, CORS origins, and locales (GAP-18).</summary>
[HasPolicy(ContentPolicies.TenantManage)]
public sealed record UpdateSiteSettingsCommand(
    Guid SiteId,
    string? PreviewUrlTemplate,
    bool VersioningEnabled,
    bool WorkflowEnabled,
    bool SchedulingEnabled,
    bool PreviewEnabled,
    bool AiEnabled,
    IReadOnlyList<string> CorsOrigins,
    IReadOnlyList<string> Locales) : ICommand<SiteSettingsDto>;

// ── Handlers ──────────────────────────────────────────────────────────────────

internal sealed class UpsertSiteEnvironmentCommandHandler(
    IRepository<Site, SiteId> siteRepository)
    : IRequestHandler<UpsertSiteEnvironmentCommand, Result<SiteEnvironmentDto>>
{
    public async Task<Result<SiteEnvironmentDto>> Handle(
        UpsertSiteEnvironmentCommand request, CancellationToken cancellationToken)
    {
        var site = await siteRepository.GetByIdAsync(new SiteId(request.SiteId), cancellationToken)
     ?? throw new NotFoundException(nameof(Site), request.SiteId);

        if (!Enum.TryParse<EnvironmentType>(request.EnvironmentType, ignoreCase: true, out var envType))
            return Result.Failure<SiteEnvironmentDto>(
                Error.Validation("Site.InvalidEnvironmentType", $"'{request.EnvironmentType}' is not a valid environment type."));

   site.AddEnvironment(envType, request.Url, request.IsLive);
 siteRepository.Update(site);

        var env = site.Environments.First(e => e.Type == envType);
    return Result.Success(new SiteEnvironmentDto(env.Type.ToString(), env.Url, env.SslStatus.ToString(), env.IsLive));
    }
}

internal sealed class UpdateSiteSettingsCommandHandler(
    IRepository<SiteSettings, SiteId> settingsRepository,
 IRepository<Site, SiteId> siteRepository)
    : IRequestHandler<UpdateSiteSettingsCommand, Result<SiteSettingsDto>>
{
    public async Task<Result<SiteSettingsDto>> Handle(
    UpdateSiteSettingsCommand request, CancellationToken cancellationToken)
    {
        _ = await siteRepository.GetByIdAsync(new SiteId(request.SiteId), cancellationToken)
 ?? throw new NotFoundException(nameof(Site), request.SiteId);

        var siteId = new SiteId(request.SiteId);
        var settings = await settingsRepository.GetByIdAsync(siteId, cancellationToken);
        if (settings is null)
     {
  var firstLocale = request.Locales.FirstOrDefault() ?? "en";
 settings = SiteSettings.CreateDefault(siteId, new TenantId(Guid.Empty), Locale.Create(firstLocale));
       await settingsRepository.AddAsync(settings, cancellationToken);
        }

    settings.UpdateFeatureFlags(
            request.VersioningEnabled, request.WorkflowEnabled,
          request.SchedulingEnabled, request.PreviewEnabled, request.AiEnabled);
   settings.SetPreviewUrlTemplate(request.PreviewUrlTemplate);
     settings.SetCorsOrigins(request.CorsOrigins);
        settings.SetLocales(request.Locales);
        settingsRepository.Update(settings);

        return Result.Success(SiteSettingsMapper.ToDto(settings));
    }
}

internal static class SiteSettingsMapper
{
    internal static SiteSettingsDto ToDto(SiteSettings s) => new(
        s.Id.Value, s.PreviewUrlTemplate,
        s.VersioningEnabled, s.WorkflowEnabled, s.SchedulingEnabled, s.PreviewEnabled, s.AiEnabled,
        s.CorsOrigins, s.Locales);
}
