using Microsoft.Extensions.Logging;
using MicroCMS.Ai.Abstractions;
using MicroCMS.Ai.Abstractions.Exceptions;
using MicroCMS.Ai.Abstractions.Interfaces;
using MicroCMS.Ai.Abstractions.Interfaces;
using MicroCMS.Shared.Ids;

namespace MicroCMS.Ai.Core.Services;

/// <summary>
/// Tracks AI token consumption and enforces budget caps per tenant/user.
/// Reads budget limits from <see cref="ISettingsReader"/> using <see cref="AiSettingKeys"/>.
/// Returns HTTP 429 (Too Many Requests) when budget is exceeded.
/// GAP-27: Budget enforcement.
/// </summary>
public sealed class BudgetService : IAiUsageTracker
{
    private readonly ISettingsReader _settingsReader;
    private readonly ILogger<BudgetService> _logger;

    // In-memory token counters per tenant/user for the current day
    // Production: replace with Redis or database-backed storage
    private readonly Dictionary<string, TokenUsage> _usageCache = new();
    private readonly object _lock = new();

    public BudgetService(
        ISettingsReader settingsReader,
        ILogger<BudgetService> logger)
    {
        _settingsReader = settingsReader;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task RecordUsageAsync(
        TenantId tenantId,
        Guid userId,
        string featureKey,
        string providerName,
        string model,
        int promptTokens,
        int completionTokens,
        decimal estimatedCostUsd,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(tenantId, nameof(tenantId));

        var totalTokens = promptTokens + completionTokens;
        var usageKey = BuildUsageKey(tenantId, userId);

        lock (_lock)
        {
            if (!_usageCache.TryGetValue(usageKey, out var usage))
            {
                usage = new TokenUsage
                {
                    Date = DateOnly.FromDateTime(DateTime.UtcNow),
                    TotalTokens = 0,
                    TotalCostUsd = 0
                };
                _usageCache[usageKey] = usage;
            }

            // Reset if it's a new day
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            if (usage.Date != today)
            {
                usage.Date = today;
                usage.TotalTokens = 0;
                usage.TotalCostUsd = 0;
            }

            usage.TotalTokens += totalTokens;
            usage.TotalCostUsd += estimatedCostUsd;
        }

        _logger.LogInformation(
            "Recorded AI usage: tenant {TenantId}, user {UserId}, feature '{Feature}', " +
            "provider '{Provider}', model '{Model}', tokens {Tokens} (prompt: {PromptTokens}, completion: {CompletionTokens}), " +
            "cost ${Cost:F4}",
            tenantId,
            userId,
            featureKey,
            providerName,
            model,
            totalTokens,
            promptTokens,
            completionTokens,
            estimatedCostUsd);

        await Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task<bool> IsWithinBudgetAsync(
        TenantId tenantId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(tenantId, nameof(tenantId));

        // Read budget limit from settings
        var maxTokensPerDay = await _settingsReader.GetAsync<long>(
            tenantId,
            siteId: null, // Tenant-level setting
            AiSettingKeys.BudgetMaxTokensPerDay,
            defaultValue: 0L, // 0 = unlimited
            cancellationToken);

        if (maxTokensPerDay == 0)
        {
            // No limit configured
            return true;
        }

        var usageKey = BuildUsageKey(tenantId, userId);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        lock (_lock)
        {
            if (!_usageCache.TryGetValue(usageKey, out var usage))
            {
                // No usage recorded yet
                return true;
            }

            // Reset if it's a new day
            if (usage.Date != today)
            {
                usage.Date = today;
                usage.TotalTokens = 0;
                usage.TotalCostUsd = 0;
                return true;
            }

            var isWithinBudget = usage.TotalTokens < maxTokensPerDay;

            if (!isWithinBudget)
            {
                _logger.LogWarning(
                    "Budget limit exceeded for tenant {TenantId}, user {UserId}: " +
                    "{CurrentTokens} tokens used, limit is {MaxTokens} per day",
                    tenantId,
                    userId,
                    usage.TotalTokens,
                    maxTokensPerDay);
            }

            return isWithinBudget;
        }
    }

    /// <summary>
    /// Throws <see cref="QuotaExceededException"/> if the tenant/user has exceeded their budget.
    /// Call this before dispatching any AI request when hard-stop is enabled.
    /// </summary>
    public async Task EnforceBudgetAsync(
        TenantId tenantId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var isWithinBudget = await IsWithinBudgetAsync(tenantId, userId, cancellationToken);

        if (!isWithinBudget)
        {
            throw new AiBudgetExceededException(
                "AI token budget exceeded for today. Please try again tomorrow or contact support to increase your limit.");
        }
    }

    /// <summary>
    /// Gets current usage statistics for a tenant/user on the current day.
    /// </summary>
    public (long TokensUsed, decimal CostUsd) GetCurrentUsage(TenantId tenantId, Guid userId)
    {
        var usageKey = BuildUsageKey(tenantId, userId);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        lock (_lock)
        {
            if (!_usageCache.TryGetValue(usageKey, out var usage) || usage.Date != today)
            {
                return (0L, 0m);
            }

            return (usage.TotalTokens, usage.TotalCostUsd);
        }
    }

    private static string BuildUsageKey(TenantId tenantId, Guid userId)
        => $"{tenantId.Value}::{userId}";

    private sealed class TokenUsage
    {
        public DateOnly Date { get; set; }
        public long TotalTokens { get; set; }
        public decimal TotalCostUsd { get; set; }
    }
}
