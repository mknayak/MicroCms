using MediatR;
using MicroCMS.Application.Common.Attributes;
using MicroCMS.Application.Common.Authorization;
using MicroCMS.Application.Common.Exceptions;
using MicroCMS.Application.Common.Interfaces;
using MicroCMS.Application.Common.Markers;
using MicroCMS.Domain.Aggregates.Settings;
using MicroCMS.Domain.Aggregates.Tenant;
using MicroCMS.Domain.Repositories;
using MicroCMS.Shared.Ids;
using MicroCMS.Shared.Results;

namespace MicroCMS.Application.Features.Sites.Commands;

// ── DTOs ──────────────────────────────────────────────────────────────────────

/// <summary>A single config entry as seen by the API consumer. Secret values are redacted.</summary>
public sealed record ConfigEntryDto(
    string Key,
    string Value,
    string Category,
    bool IsSecret,
    DateTimeOffset UpdatedAt);

/// <summary>A page of config entries for a site, optionally scoped to a category.</summary>
public sealed record SiteConfigEntriesDto(
    Guid SiteId,
    IReadOnlyList<ConfigEntryDto> Entries);

/// <summary>One entry in a bulk import/export payload.</summary>
public sealed record ImportConfigEntry(
    string Key,
    string Value,
    string Category = "general",
    bool IsSecret = false);

// ── Mapper (shared between commands and queries) ──────────────────────────────

internal static class ConfigEntryMapper
{
    internal static ConfigEntryDto ToDto(ConfigEntry e) => new(
        e.Key,
        e.IsSecret ? "***" : e.Value,
        e.Category,
        e.IsSecret,
        e.UpdatedAt);
}

// ── Commands ──────────────────────────────────────────────────────────────────

/// <summary>
/// Inserts or replaces a single config entry for a site.
/// Pass <c>IsSecret = true</c> for credentials — the value will be redacted in read responses.
/// </summary>
[HasPolicy(ContentPolicies.TenantManage)]
public sealed record UpsertSiteConfigEntryCommand(
    Guid SiteId,
    string Key,
    string Value,
    string Category = "general",
    bool IsSecret = false) : ICommand<ConfigEntryDto>;

/// <summary>Removes a config entry from a site. No-op if the key does not exist.</summary>
[HasPolicy(ContentPolicies.TenantManage)]
public sealed record DeleteSiteConfigEntryCommand(
    Guid SiteId,
    string Key) : ICommand;

/// <summary>
/// Bulk-imports config entries from an import payload (e.g. ai.settings.json).
/// Non-destructive merge: keys absent from the payload are left untouched.
/// </summary>
[HasPolicy(ContentPolicies.TenantManage)]
public sealed record BulkImportSiteConfigEntriesCommand(
    Guid SiteId,
    IReadOnlyList<ImportConfigEntry> Entries) : ICommand<SiteConfigEntriesDto>;

// ── Handlers ──────────────────────────────────────────────────────────────────

internal sealed class UpsertSiteConfigEntryCommandHandler(
    IRepository<SiteSettings, SiteId> settingsRepo,
    ISettingsReader settingsReader)
    : IRequestHandler<UpsertSiteConfigEntryCommand, Result<ConfigEntryDto>>
{
    public async Task<Result<ConfigEntryDto>> Handle(
        UpsertSiteConfigEntryCommand request, CancellationToken cancellationToken)
    {
        var siteId = new SiteId(request.SiteId);
        var settings = await settingsRepo.GetByIdAsync(siteId, cancellationToken);
        if (settings is null)
            return Result.Failure<ConfigEntryDto>(
                Error.NotFound("SiteSettings.NotFound", $"Site settings not found for site {request.SiteId}."));

        settings.UpsertEntry(request.Key, request.Value, request.Category, request.IsSecret);
        settingsRepo.Update(settings);
        await settingsReader.InvalidateAsync(siteId, cancellationToken);

        return Result.Success(ConfigEntryMapper.ToDto(settings.GetEntry(request.Key)!));
    }
}

internal sealed class DeleteSiteConfigEntryCommandHandler(
    IRepository<SiteSettings, SiteId> settingsRepo,
    ISettingsReader settingsReader)
    : IRequestHandler<DeleteSiteConfigEntryCommand, Result>
{
    public async Task<Result> Handle(
        DeleteSiteConfigEntryCommand request, CancellationToken cancellationToken)
    {
        var siteId = new SiteId(request.SiteId);
        var settings = await settingsRepo.GetByIdAsync(siteId, cancellationToken);
        if (settings is null)
            throw new NotFoundException(nameof(SiteSettings), request.SiteId);

        settings.RemoveEntry(request.Key);
        settingsRepo.Update(settings);
        await settingsReader.InvalidateAsync(siteId, cancellationToken);
        return Result.Success();
    }
}

internal sealed class BulkImportSiteConfigEntriesCommandHandler(
    IRepository<SiteSettings, SiteId> settingsRepo,
    ISettingsReader settingsReader)
    : IRequestHandler<BulkImportSiteConfigEntriesCommand, Result<SiteConfigEntriesDto>>
{
    public async Task<Result<SiteConfigEntriesDto>> Handle(
        BulkImportSiteConfigEntriesCommand request, CancellationToken cancellationToken)
    {
        var siteId = new SiteId(request.SiteId);
        var settings = await settingsRepo.GetByIdAsync(siteId, cancellationToken);
        if (settings is null)
            return Result.Failure<SiteConfigEntriesDto>(
                Error.NotFound("SiteSettings.NotFound", $"Site settings not found for site {request.SiteId}."));

        foreach (var entry in request.Entries)
            settings.UpsertEntry(entry.Key, entry.Value, entry.Category, entry.IsSecret);

        settingsRepo.Update(settings);
        await settingsReader.InvalidateAsync(siteId, cancellationToken);

        return Result.Success(new SiteConfigEntriesDto(
            request.SiteId,
            settings.ConfigEntries.Select(ConfigEntryMapper.ToDto).ToList()));
    }
}
