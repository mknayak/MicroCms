namespace MicroCMS.Application.Common.Interfaces;

/// <summary>
/// Abstraction over system clock — enables deterministic testing.
/// </summary>
public interface IDateTimeProvider
{
    DateTimeOffset UtcNow { get; }
}
