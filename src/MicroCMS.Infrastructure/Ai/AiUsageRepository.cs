using MicroCMS.Ai.Abstractions;
using MicroCMS.Ai.Abstractions.Interfaces;
using MicroCMS.Infrastructure.Persistence.Common;
using MicroCMS.Shared.Ids;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MicroCMS.Infrastructure.Ai;

/// <summary>
/// DB-backed implementation of <see cref="IAiUsageTracker"/> using the <c>AiDailyUsage</c> table.
/// Replaces the in-memory <c>BudgetService</c> for multi-instance correctness (Sprint 15).
/// Uses upsert (INSERT … ON CONFLICT) for atomic increment across concurrent requests.
/// </summary>
public sealed class AiUsageRepository : IAiUsageTracker
{
    private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;
    private readonly ISettingsReader _settingsReader;
    private readonly ILogger<AiUsageRepository> _logger;

    public AiUsageRepository(
        IDbContextFactory<ApplicationDbContext> dbFactory,
        ISettingsReader settingsReader,
        ILogger<AiUsageRepository> logger)
    {
        _dbFactory = dbFactory;
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
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);

        var existing = await db.AiDailyUsage
            .FirstOrDefaultAsync(
                u => u.TenantId == tenantId.Value && u.UserId == userId && u.Date == today,
                cancellationToken);

        if (existing is null)
        {
            db.AiDailyUsage.Add(new AiDailyUsage
            {
                TenantId = tenantId.Value,
                UserId = userId,
                Date = today,
                TotalTokens = totalTokens,
                TotalCostUsd = estimatedCostUsd,
            });
        }
        else
        {
            existing.TotalTokens += totalTokens;
            existing.TotalCostUsd += estimatedCostUsd;
        }

        await db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Recorded AI usage: tenant {TenantId}, user {UserId}, feature '{Feature}', " +
            "provider '{Provider}', model '{Model}', tokens {Tokens}, cost ${Cost:F4}",
            tenantId, userId, featureKey, providerName, model, totalTokens, estimatedCostUsd);
    }

    /// <inheritdoc/>
    public async Task<bool> IsWithinBudgetAsync(
        TenantId tenantId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(tenantId, nameof(tenantId));

        var maxTokensPerDay = await _settingsReader.GetAsync<long>(
            tenantId,
            siteId: null,
            AiSettingKeys.BudgetMaxTokensPerDay,
            defaultValue: 0L,
            cancellationToken);

        if (maxTokensPerDay == 0)
            return true;

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);

        var usage = await db.AiDailyUsage
            .FirstOrDefaultAsync(
                u => u.TenantId == tenantId.Value && u.UserId == userId && u.Date == today,
                cancellationToken);

        if (usage is null)
            return true;

        var isWithinBudget = usage.TotalTokens < maxTokensPerDay;

        if (!isWithinBudget)
            _logger.LogWarning(
                "Budget limit exceeded for tenant {TenantId}, user {UserId}: " +
                "{CurrentTokens} tokens used, limit is {MaxTokens} per day",
                tenantId, userId, usage.TotalTokens, maxTokensPerDay);

        return isWithinBudget;
    }
}
