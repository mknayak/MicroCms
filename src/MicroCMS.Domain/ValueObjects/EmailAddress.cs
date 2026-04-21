using System.Net.Mail;
using MicroCMS.Domain.Exceptions;

namespace MicroCMS.Domain.ValueObjects;

/// <summary>
/// Validated email address value object.
/// Delegates format validation to <see cref="MailAddress"/> (BCL) to avoid
/// re-implementing RFC 5321/5322 parsing rules.
/// Stored in lower-case for consistent comparison.
/// </summary>
public sealed class EmailAddress : ValueObject
{
    public const int MaxLength = 254; // RFC 5321 maximum

    private EmailAddress(string value) => Value = value;

    public string Value { get; }

    public static EmailAddress Create(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, nameof(value));

        if (value.Length > MaxLength)
        {
            throw new DomainException($"Email address must not exceed {MaxLength} characters.");
        }

        try
        {
            var addr = new MailAddress(value.Trim());
            return new EmailAddress(addr.Address.ToLowerInvariant());
        }
        catch (FormatException)
        {
            throw new DomainException($"'{value}' is not a valid email address.");
        }
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;
}
