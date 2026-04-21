using System.Text.RegularExpressions;
using MicroCMS.Domain.Exceptions;

namespace MicroCMS.Domain.ValueObjects;

/// <summary>
/// URL-safe slug value object. Enforces a strict whitelist: lowercase letters,
/// digits, and hyphens only. No leading/trailing hyphens. Max 200 characters.
/// Security: whitelist approach — any character outside the allowed set is rejected
/// rather than silently stripped, preventing ambiguous slug collisions.
/// </summary>
public sealed class Slug : ValueObject
{
    private static readonly Regex ValidPattern =
        new(@"^[a-z0-9]+(?:-[a-z0-9]+)*$", RegexOptions.Compiled, TimeSpan.FromMilliseconds(100));

    public const int MaxLength = 200;

    private Slug(string value) => Value = value;

    public string Value { get; }

    public static Slug Create(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, nameof(value));

        if (value.Length > MaxLength)
        {
            throw new DomainException(
                $"Slug must not exceed {MaxLength} characters. Got {value.Length}.");
        }

        if (!ValidPattern.IsMatch(value))
        {
            throw new DomainException(
                "Slug may only contain lowercase letters, digits, and hyphens, " +
                "and must not start or end with a hyphen.");
        }

        return new Slug(value);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;
}
