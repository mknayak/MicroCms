using System.Text.RegularExpressions;
using MicroCMS.Domain.Exceptions;

namespace MicroCMS.Domain.ValueObjects;

/// <summary>
/// BCP 47 locale code value object (e.g. "en", "en-US", "zh-Hant-TW").
/// Used to key locale-specific entry content and locale fallback chains.
/// </summary>
public sealed class Locale : ValueObject
{
    // Simplified BCP 47: language[‑script][‑region]
    private static readonly Regex ValidPattern =
        new(@"^[a-zA-Z]{2,8}(-[a-zA-Z0-9]{1,8}){0,3}$",
            RegexOptions.Compiled,
            TimeSpan.FromMilliseconds(100));

    public const int MaxLength = 35;

    private Locale(string value) => Value = value;

    public string Value { get; }

    public static Locale Create(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, nameof(value));

        if (value.Length > MaxLength)
        {
            throw new DomainException($"Locale code must not exceed {MaxLength} characters.");
        }

        if (!ValidPattern.IsMatch(value))
        {
            throw new DomainException(
                $"'{value}' is not a valid BCP 47 locale code (e.g. 'en', 'en-US').");
        }

        return new Locale(value);
    }

    public static readonly Locale EnglishUS = Create("en-US");
    public static readonly Locale English = Create("en");

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value.ToLowerInvariant();
    }

    public override string ToString() => Value;
}
