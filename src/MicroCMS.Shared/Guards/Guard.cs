using System.Runtime.CompilerServices;

namespace MicroCMS.Shared.Guards;

/// <summary>
/// Lightweight guard clause helpers that throw <see cref="ArgumentException"/>-derived
/// exceptions for invalid inputs. Keeps handler/service code free of repetitive null checks.
/// </summary>
public static class Guard
{
    /// <summary>Throws <see cref="ArgumentNullException"/> when <paramref name="value"/> is null.</summary>
    public static T AgainstNull<T>(
        T? value,
        [CallerArgumentExpression(nameof(value))] string paramName = "")
        where T : class
    {
        ArgumentNullException.ThrowIfNull(value, paramName);
        return value;
    }

    /// <summary>Throws <see cref="ArgumentException"/> when <paramref name="value"/> is null or whitespace.</summary>
    public static string AgainstNullOrWhiteSpace(
        string? value,
        [CallerArgumentExpression(nameof(value))] string paramName = "")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, paramName);
        return value;
    }

    /// <summary>Throws <see cref="ArgumentOutOfRangeException"/> when <paramref name="value"/> is negative.</summary>
    public static int AgainstNegative(
        int value,
        [CallerArgumentExpression(nameof(value))] string paramName = "")
    {
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException(paramName, value, "Value must be non-negative.");
        }

        return value;
    }

    /// <summary>Throws <see cref="ArgumentOutOfRangeException"/> when <paramref name="value"/> is empty.</summary>
    public static Guid AgainstEmptyGuid(
        Guid value,
        [CallerArgumentExpression(nameof(value))] string paramName = "")
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentOutOfRangeException(paramName, "Guid value must not be empty.");
        }

        return value;
    }
}
