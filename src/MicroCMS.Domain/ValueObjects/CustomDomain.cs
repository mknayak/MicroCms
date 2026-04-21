using System.Text.RegularExpressions;
using MicroCMS.Domain.Exceptions;

namespace MicroCMS.Domain.ValueObjects;

/// <summary>
/// Represents a validated custom hostname (e.g. "cms.acme.com").
/// Enforces RFC 1123 hostname rules: max 253 characters, labels 1–63 chars,
/// alphanumeric and hyphens only, no leading/trailing hyphens per label.
/// </summary>
public sealed class CustomDomain : ValueObject
{
    // Hostname: each label is 1-63 chars of [a-z0-9-], total ≤ 253 chars
    private static readonly Regex ValidPattern =
        new(@"^(?!-)[a-z0-9-]{1,63}(?<!-)(\.[a-z0-9-]{1,63}(?<!-))*$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase,
            TimeSpan.FromMilliseconds(100));

    public const int MaxLength = 253;

    private CustomDomain(string value) => Value = value.ToLowerInvariant();

    public string Value { get; }

    public static CustomDomain Create(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, nameof(value));

        if (value.Length > MaxLength)
        {
            throw new DomainException($"Custom domain must not exceed {MaxLength} characters.");
        }

        if (!ValidPattern.IsMatch(value))
        {
            throw new DomainException(
                "Custom domain must be a valid hostname (e.g. 'cms.acme.com').");
        }

        return new CustomDomain(value);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;
}
