using MicroCMS.Domain.Exceptions;

namespace MicroCMS.Domain.ValueObjects;

/// <summary>
/// Immutable configuration snapshot for a tenant.
/// Changing settings produces a new instance (value object semantics).
/// </summary>
public sealed class TenantSettings : ValueObject
{
    public const int MaxDisplayNameLength = 200;
    public const int MaxTimeZoneIdLength = 64;

    private TenantSettings(
        string displayName,
        Locale defaultLocale,
        IReadOnlyList<Locale> enabledLocales,
        string timeZoneId,
        bool aiEnabled,
        string? logoUrl)
    {
        DisplayName = displayName;
        DefaultLocale = defaultLocale;
        EnabledLocales = enabledLocales;
        TimeZoneId = timeZoneId;
        AiEnabled = aiEnabled;
        LogoUrl = logoUrl;
    }

    public string DisplayName { get; }
    public Locale DefaultLocale { get; }
    public IReadOnlyList<Locale> EnabledLocales { get; }
    public string TimeZoneId { get; }
    public bool AiEnabled { get; }
    public string? LogoUrl { get; }

    public static TenantSettings Create(
        string displayName,
        Locale defaultLocale,
        IReadOnlyList<Locale>? enabledLocales = null,
        string timeZoneId = "UTC",
        bool aiEnabled = false,
        string? logoUrl = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName, nameof(displayName));

        if (displayName.Length > MaxDisplayNameLength)
        {
            throw new DomainException(
                $"Display name must not exceed {MaxDisplayNameLength} characters.");
        }

        ArgumentNullException.ThrowIfNull(defaultLocale, nameof(defaultLocale));

        if (timeZoneId.Length > MaxTimeZoneIdLength)
        {
            throw new DomainException(
                $"Time zone ID must not exceed {MaxTimeZoneIdLength} characters.");
        }

        var locales = enabledLocales ?? new List<Locale> { defaultLocale };

        return new TenantSettings(
            displayName.Trim(),
            defaultLocale,
            locales,
            timeZoneId,
            aiEnabled,
            logoUrl);
    }

    /// <summary>Returns a new instance with only the specified properties changed.</summary>
    public TenantSettings With(
        string? displayName = null,
        Locale? defaultLocale = null,
        IReadOnlyList<Locale>? enabledLocales = null,
        string? timeZoneId = null,
        bool? aiEnabled = null,
        string? logoUrl = null)
        => Create(
            displayName ?? DisplayName,
            defaultLocale ?? DefaultLocale,
            enabledLocales ?? EnabledLocales,
            timeZoneId ?? TimeZoneId,
            aiEnabled ?? AiEnabled,
            logoUrl ?? LogoUrl);

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return DisplayName;
        yield return DefaultLocale;
        yield return TimeZoneId;
        yield return AiEnabled;
        yield return LogoUrl;
        foreach (var l in EnabledLocales)
        {
            yield return l;
        }
    }
}
