namespace MicroCMS.Infrastructure.Ai;

/// <summary>
/// Infrastructure record that stores per-tenant, per-user, per-day AI token consumption.
/// Replaces the in-memory dictionary in <see cref="MicroCMS.Ai.Core.Services.BudgetService"/> so
/// usage survives restarts and is accurate across multi-instance deployments (Sprint 15).
/// </summary>
public sealed class AiDailyUsage
{
    public Guid TenantId { get; set; }
    public Guid UserId { get; set; }

    /// <summary>UTC date (no time component).</summary>
    public DateOnly Date { get; set; }

    public long TotalTokens { get; set; }
    public decimal TotalCostUsd { get; set; }
}
