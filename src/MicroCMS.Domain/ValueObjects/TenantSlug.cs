using System.Text.RegularExpressions;
using MicroCMS.Domain.Exceptions;

namespace MicroCMS.Domain.ValueObjects;

/// <summary>
/// Subdomain-safe tenant identifier.
/// Constraints: 3–63 characters, lowercase alphanumeric and hyphens, no leading/trailing hyphens.
/// Mirrors DNS label rules (RFC 1035) so the value can be used directly as a subdomain.
/// </summary>
public sealed class TenantSlug : ValueObject
{
    private static readonly Regex ValidPattern =
        new(@"^[a-z0-9]([a-z0-9-]{1,61}[a-z0-9])?$", RegexOptions.Compiled, TimeSpan.FromMilliseconds(100));

    public const int MinLength = 3;
    public const int MaxLength = 63;

    private TenantSlug(string value) => Value = value;

    public string Value { get; }

    public static TenantSlug Create(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, nameof(value));

        if (value.Length < MinLength || value.Length > MaxLength)
        {
            throw new DomainException(
                $"TenantSlug must be between {MinLength} and {MaxLength} characters.");
        }

        if (!ValidPattern.IsMatch(value))
        {
            throw new DomainException(
                "TenantSlug may only contain lowercase letters, digits, and hyphens, " +
                "and must not start or end with a hyphen.");
        }

        return new TenantSlug(value);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;
}
