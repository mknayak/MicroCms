using MicroCMS.Domain.Exceptions;
using MicroCMS.Shared.Ids;

namespace MicroCMS.Domain.Aggregates.Ai;

/// <summary>Strongly-typed ID for <c>AiProviderSettings</c>.</summary>
public readonly record struct AiProviderSettingsId(Guid Value)
{
  public static AiProviderSettingsId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString();
}

/// <summary>
/// Per-tenant AI provider configuration aggregate (GAP-27).
/// Stores which provider is active, per-feature model tier overrides,
/// budget caps, and safety configuration.
/// </summary>
public sealed class AiProviderSettings : AggregateRoot<AiProviderSettingsId>
{
    private readonly List<AiModelTierOverride> _modelOverrides = [];

    private AiProviderSettings() : base() { } // EF Core

    private AiProviderSettings(AiProviderSettingsId id, TenantId tenantId, string activeProvider)
        : base(id)
    {
    TenantId = tenantId;
     ActiveProvider = activeProvider;
  UpdatedAt = DateTimeOffset.UtcNow;
    }

    public TenantId TenantId { get; private set; }
    /// <summary>Provider name key: "anthropic", "azure_openai", "openai", "bedrock", "ollama", "vertex".</summary>
    public string ActiveProvider { get; private set; } = string.Empty;
 public AiBudget Budget { get; private set; } = AiBudget.Default;
    public AiSafetyConfig Safety { get; private set; } = AiSafetyConfig.Default;
    public DateTimeOffset UpdatedAt { get; private set; }
    public IReadOnlyList<AiModelTierOverride> ModelOverrides => _modelOverrides.AsReadOnly();

    public static AiProviderSettings Create(TenantId tenantId, string activeProvider)
    {
     ArgumentException.ThrowIfNullOrWhiteSpace(activeProvider, nameof(activeProvider));
        return new AiProviderSettings(AiProviderSettingsId.New(), tenantId, activeProvider.ToLowerInvariant());
    }

  public void SetActiveProvider(string provider)
    {
  ArgumentException.ThrowIfNullOrWhiteSpace(provider, nameof(provider));
    ActiveProvider = provider.ToLowerInvariant();
  UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void SetBudget(AiBudget budget)
 {
  ArgumentNullException.ThrowIfNull(budget);
 Budget = budget;
    UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void SetSafety(AiSafetyConfig safety)
    {
    ArgumentNullException.ThrowIfNull(safety);
   Safety = safety;
  UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void UpsertModelOverride(string featureKey, string model)
    {
     ArgumentException.ThrowIfNullOrWhiteSpace(featureKey, nameof(featureKey));
    _modelOverrides.RemoveAll(o => o.FeatureKey == featureKey);
   _modelOverrides.Add(new AiModelTierOverride(featureKey, model));
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}

/// <summary>Overrides the default model for a specific feature (GAP-27).</summary>
public sealed record AiModelTierOverride(string FeatureKey, string Model);

/// <summary>
/// Monthly cost cap and per-user daily token budget (GAP-27).
/// HardStop = true means requests are rejected when the cap is reached.
/// </summary>
public sealed class AiBudget
{
    public decimal MonthlyCostCapUsd { get; init; }
    public int PerUserDailyTokenCap { get; init; }
    public bool HardStop { get; init; }
    public decimal CurrentMonthSpendUsd { get; init; }

    public static AiBudget Default => new()
  {
   MonthlyCostCapUsd = 100m,
        PerUserDailyTokenCap = 50_000,
    HardStop = false
    };
}

/// <summary>AI safety and data-handling configuration for a tenant (GAP-27).</summary>
public sealed class AiSafetyConfig
{
    public bool PiiRedactionEnabled { get; init; } = true;
    public bool PromptInjectionDetectionEnabled { get; init; } = true;
    public bool SafetyPostFilterEnabled { get; init; } = true;
    public bool GroundedOnlyModeDefault { get; init; }
    /// <summary>ISO 3166-1 alpha-2 country code constraining data residency, e.g. "US", "EU".</summary>
    public string? DataResidencyRegion { get; init; }
    /// <summary>Audit log retention in days.</summary>
 public int AuditLogRetentionDays { get; init; } = 90;

    public static AiSafetyConfig Default => new();
}
