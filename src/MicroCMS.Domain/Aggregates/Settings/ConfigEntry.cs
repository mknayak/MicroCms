using MicroCMS.Domain.Entities;
using MicroCMS.Domain.Exceptions;
using MicroCMS.Shared.Ids;

namespace MicroCMS.Domain.Aggregates.Settings;

/// <summary>
/// A single key-value configuration entry owned by either
/// <see cref="TenantConfig"/> (tenant scope) or
/// <see cref="MicroCMS.Domain.Aggregates.Tenant.SiteSettings"/> (site scope).
///
/// Secrets (e.g. API keys) are flagged with <see cref="IsSecret"/> so the
/// read API can redact them; encryption at rest is applied by the infrastructure layer.
/// </summary>
public sealed class ConfigEntry : Entity<ConfigEntryId>
{
    public const int MaxKeyLength = 200;
    public const int MaxValueLength = 4000;
    public const int MaxCategoryLength = 100;

    private ConfigEntry() : base() { } // EF Core

    private ConfigEntry(ConfigEntryId id, string key, string value, string category, bool isSecret)
        : base(id)
    {
        Key = key;
        Value = value;
        Category = category;
        IsSecret = isSecret;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public string Key { get; private set; } = string.Empty;
    public string Value { get; private set; } = string.Empty;

    /// <summary>Logical grouping for Admin UI (e.g. "ai", "media", "webhooks").</summary>
    public string Category { get; private set; } = "general";

    /// <summary>When <c>true</c> the value is redacted in read API responses.</summary>
    public bool IsSecret { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    internal static ConfigEntry Create(string key, string value, string category, bool isSecret)
    {
        Validate(key, value, category);
        return new ConfigEntry(ConfigEntryId.New(), key, value, category, isSecret);
    }

    internal void Update(string value, string category, bool isSecret)
    {
        if (value.Length > MaxValueLength)
            throw new DomainException($"Config entry value must not exceed {MaxValueLength} characters.");

        Value = value;
        Category = category;
        IsSecret = isSecret;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    private static void Validate(string key, string value, string category)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key, nameof(key));

        if (key.Length > MaxKeyLength)
            throw new DomainException($"Config entry key must not exceed {MaxKeyLength} characters.");

        ArgumentNullException.ThrowIfNull(value, nameof(value));

        if (value.Length > MaxValueLength)
            throw new DomainException($"Config entry value must not exceed {MaxValueLength} characters.");

        if (!string.IsNullOrWhiteSpace(category) && category.Length > MaxCategoryLength)
            throw new DomainException($"Config entry category must not exceed {MaxCategoryLength} characters.");
    }
}
