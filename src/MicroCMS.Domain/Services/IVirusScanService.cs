namespace MicroCMS.Domain.Services;

/// <summary>
/// Abstracts virus/malware scanning for uploaded media assets (GAP-15).
/// Concrete implementations live in Infrastructure (ClamAV, cloud-based scanners).
/// Mapped to the plugin capability "storage.scan" in the plugin registry.
/// </summary>
public interface IVirusScanService
{
    /// <summary>
    /// Scans the byte stream identified by <paramref name="storageKey"/>.
    /// Returns a <see cref="VirusScanResult"/> indicating whether the file is clean or infected.
    /// </summary>
    Task<VirusScanResult> ScanAsync(string storageKey, CancellationToken cancellationToken = default);
}

/// <summary>Outcome of a virus scan operation.</summary>
public sealed record VirusScanResult(
    bool IsClean,
    string? ThreatName = null,
    string? ScannerVersion = null);
