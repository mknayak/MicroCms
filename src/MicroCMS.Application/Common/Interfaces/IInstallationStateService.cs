namespace MicroCMS.Application.Common.Interfaces;

/// <summary>
/// Reports whether the application has been installed (i.e., at least one tenant exists).
///
/// Implementations MUST cache the result in memory after the first call so that the check
/// costs zero database round-trips during normal operation. Once the system is installed the
/// value can never go back to <c>false</c>, so the cache never needs to be invalidated.
/// </summary>
public interface IInstallationStateService
{
    /// <summary>
    /// Returns <c>true</c> when the system has been installed; <c>false</c> on first run.
    /// The result is cached in-process after the first successful database check.
    /// </summary>
    Task<bool> IsInstalledAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Called by the install handler after it successfully persists the first tenant.
    /// Primes the in-memory cache so subsequent calls to <see cref="IsInstalledAsync"/>
    /// return <c>true</c> without hitting the database.
    /// </summary>
    void MarkInstalled();
}
