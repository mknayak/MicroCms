using MicroCMS.Application.Common.Interfaces;

namespace MicroCMS.Infrastructure.Identity;

/// <summary>
/// Production implementation of <see cref="IDateTimeProvider"/> that delegates to the
/// system clock (<see cref="DateTimeOffset.UtcNow"/>).
///
/// Tests should use a mock/stub of <see cref="IDateTimeProvider"/> for determinism.
/// </summary>
internal sealed class SystemDateTimeProvider : IDateTimeProvider
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
