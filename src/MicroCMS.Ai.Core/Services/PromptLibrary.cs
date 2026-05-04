using Microsoft.Extensions.Logging;
using MicroCMS.Ai.Abstractions;
using MicroCMS.Ai.Abstractions.Interfaces;
using MicroCMS.Shared.Ids;

namespace MicroCMS.Ai.Core.Services;

/// <summary>
/// Resolves AI prompts exclusively from site settings — no prompts are hardcoded in C#.
///
/// Prompts are stored as <c>ConfigEntry</c> rows on <c>SiteSettings</c> using the well-known
/// keys defined in <see cref="AiSettingKeys"/>:
/// <list type="bullet">
///   <item><see cref="AiSettingKeys.EntryContentSystemPrompt"/></item>
///   <item><see cref="AiSettingKeys.EntryContentUserPrompt"/></item>
///   <item><see cref="AiSettingKeys.PageSeoSystemPrompt"/></item>
///   <item><see cref="AiSettingKeys.PageSeoUserPrompt"/></item>
/// </list>
///
/// Seed initial values via the import wizard using the reference <c>ai.settings.json</c> file,
/// or configure them manually in Site Settings → AI Settings tab.
/// </summary>
public sealed class PromptLibrary
{
    private readonly ISettingsReader _settingsReader;
    private readonly ILogger<PromptLibrary> _logger;

    public PromptLibrary(ISettingsReader settingsReader, ILogger<PromptLibrary> logger)
    {
        _settingsReader = settingsReader;
        _logger = logger;
    }

    /// <summary>
    /// Returns the entry content system prompt for the given site.
    /// Throws <see cref="InvalidOperationException"/> if not configured.
    /// </summary>
    public Task<string> GetEntryContentSystemPromptAsync(SiteId siteId, CancellationToken ct = default) =>
        RequirePromptAsync(siteId, AiSettingKeys.EntryContentSystemPrompt, ct);

    /// <summary>
    /// Returns the entry content user prompt template for the given site.
    /// Throws <see cref="InvalidOperationException"/> if not configured.
    /// </summary>
    public Task<string> GetEntryContentUserPromptAsync(SiteId siteId, CancellationToken ct = default) =>
        RequirePromptAsync(siteId, AiSettingKeys.EntryContentUserPrompt, ct);

    /// <summary>
    /// Returns the page SEO system prompt for the given site.
    /// Throws <see cref="InvalidOperationException"/> if not configured.
    /// </summary>
    public Task<string> GetPageSeoSystemPromptAsync(SiteId siteId, CancellationToken ct = default) =>
        RequirePromptAsync(siteId, AiSettingKeys.PageSeoSystemPrompt, ct);

    /// <summary>
    /// Returns the page SEO user prompt template for the given site.
    /// Throws <see cref="InvalidOperationException"/> if not configured.
    /// </summary>
    public Task<string> GetPageSeoUserPromptAsync(SiteId siteId, CancellationToken ct = default) =>
        RequirePromptAsync(siteId, AiSettingKeys.PageSeoUserPrompt, ct);

    /// <summary>
    /// Resolves any prompt by its exact settings key.
    /// Returns <c>null</c> if absent rather than throwing — use for optional prompts.
    /// </summary>
    public Task<string?> TryGetPromptAsync(SiteId siteId, string settingsKey, CancellationToken ct = default) =>
        _settingsReader.GetAsync(siteId, settingsKey, ct);

    /// <summary>All required prompt keys — AI features unavailable until all are configured.</summary>
    public IReadOnlySet<string> RequiredPromptKeys => AiSettingKeys.PromptKeys;

    // ── Private ───────────────────────────────────────────────────────────

    private async Task<string> RequirePromptAsync(SiteId siteId, string key, CancellationToken ct)
    {
        var value = await _settingsReader.GetAsync(siteId, key, ct);

        if (!string.IsNullOrWhiteSpace(value))
        {
            _logger.LogDebug("Resolved AI prompt '{Key}' for site {SiteId}", key, siteId);
            return value;
        }

        _logger.LogWarning(
            "AI prompt '{Key}' is not configured for site {SiteId}. " +
            "Import ai.settings.json or set the value in Site Settings → AI Settings.",
            key, siteId);

        throw new InvalidOperationException(
            $"AI prompt '{key}' is not configured for site {siteId.Value}. " +
            "Seed it by importing ai.settings.json through the Admin UI (Site Settings → AI Settings → Import).");
    }
}
