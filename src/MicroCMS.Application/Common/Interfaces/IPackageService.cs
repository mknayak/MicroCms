using MicroCMS.Application.Features.PackageManager.Dtos;

namespace MicroCMS.Application.Common.Interfaces;

/// <summary>
/// Orchestrates export and import of full site packages (ZIP archives).
/// The concrete implementation lives in Infrastructure and has access to EF Core.
/// </summary>
public interface IPackageService
{
    /// <summary>Builds a ZIP archive containing all selected artefacts for the given site.</summary>
    Task<byte[]> ExportAsync(ExportOptions options, CancellationToken cancellationToken = default);

    /// <summary>Reads a ZIP archive and returns statistics without persisting anything.</summary>
    Task<PackageAnalysisResult> AnalyseAsync(
        byte[] zipBytes,
        Guid targetTenantId,
        Guid targetSiteId,
        CancellationToken cancellationToken = default);

    /// <summary>Applies the package to the target site, honouring <see cref="ImportOptions"/>.</summary>
    Task<ImportProgress> ImportAsync(
        byte[] zipBytes,
        Guid targetTenantId,
        Guid targetSiteId,
        ImportOptions options,
        CancellationToken cancellationToken = default);
}
