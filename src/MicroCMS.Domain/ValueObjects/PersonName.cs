using MicroCMS.Domain.Exceptions;

namespace MicroCMS.Domain.ValueObjects;

/// <summary>
/// Display name for a user. Enforces non-empty and maximum length.
/// Does not restrict to ASCII to support international names.
/// </summary>
public sealed class PersonName : ValueObject
{
    public const int MaxLength = 200;

    private PersonName(string value) => Value = value.Trim();

    public string Value { get; }

    public static PersonName Create(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, nameof(value));

        var trimmed = value.Trim();

        if (trimmed.Length > MaxLength)
        {
            throw new DomainException(
                $"Person name must not exceed {MaxLength} characters.");
        }

        return new PersonName(trimmed);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;
}
