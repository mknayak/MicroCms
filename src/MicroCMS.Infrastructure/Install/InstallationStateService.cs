using MicroCMS.Application.Common.Interfaces;
using MicroCMS.Infrastructure.Persistence.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MicroCMS.Infrastructure.Install;

/// <summary>
/// Checks whether the system has been installed by querying the <c>Tenants</c> table once.
/// The result is cached in-process as a simple <c>volatile bool</c> — once the system is
/// installed the value never changes back to <c>false</c>, so no cache invalidation is needed.
///
/// Thread safety: the <c>volatile</c> keyword guarantees that reads from any thread always
/// see the latest write. A tiny window exists where two concurrent first-run requests could
/// both see <c>false</c> and attempt installation simultaneously; the unique constraint on
/// <c>Tenants.Slug</c> and the <c>ConflictException</c> check in the handler protect against
/// this at the application level.
/// </summary>
internal sealed class InstallationStateService(
    ApplicationDbContext dbContext,
    ILogger<InstallationStateService> logger) : IInstallationStateService
{
    // Cached in-process. Volatile so all threads observe the write immediately.
    private static volatile bool _installed;

  /// <inheritdoc/>
    public async Task<bool> IsInstalledAsync(CancellationToken cancellationToken = default)
    {
        // Fast path — already confirmed installed
        if (_installed) return true;

     logger.LogDebug("Checking installation state against database.");

        var hasTenant = await dbContext.Tenants
   .AnyAsync(cancellationToken);

  if (hasTenant)
        {
            _installed = true;
    logger.LogInformation("Installation state: installed.");
        }
        else
        {
   logger.LogInformation("Installation state: not installed — system awaiting first-run setup.");
        }

      return hasTenant;
    }

    /// <inheritdoc/>
    public void MarkInstalled()
    {
        _installed = true;
 logger.LogInformation("Installation state primed: system is now installed.");
    }
}
