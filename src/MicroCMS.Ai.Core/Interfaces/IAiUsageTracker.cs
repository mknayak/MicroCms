using MicroCMS.Shared.Ids;

namespace MicroCMS.Ai.Core.Interfaces;

/// <summary>
/// Records AI token consumption per request and enforces budget caps (GAP-27).
/// Implementations write to the audit log and update <c>AiProviderSettings.Budget.CurrentMonthSpendUsd</c>.
/// </summary>
public interface IAiUsageTracker
{
    /// <summary>Records token usage after a successful AI request.</summary>
    Task RecordUsageAsync(
        TenantId tenantId,
        Guid userId,
        string featureKey,
        string providerName,
        string model,
        int promptTokens,
        int completionTokens,
       decimal estimatedCostUsd,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns true if the tenant/user is within budget limits.
    /// Must be called before dispatching any AI request when HardStop is enabled.
    /// </summary>
    Task<bool> IsWithinBudgetAsync(
        TenantId tenantId,
        Guid userId,
  CancellationToken cancellationToken = default);
}
