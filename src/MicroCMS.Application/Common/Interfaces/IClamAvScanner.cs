namespace MicroCMS.Application.Common.Interfaces;

/// <summary>Result returned by <see cref="IClamAvScanner.ScanAsync"/>.</summary>
/// <param name="IsClean">True when no threat was detected.</param>
/// <param name="ThreatName">Populated with the signature name when <see cref="IsClean"/> is false.</param>
public sealed record ScanResult(bool IsClean, string? ThreatName);

/// <summary>
/// Virus / malware scanner abstraction.
/// The default implementation sends a byte stream to a ClamAV daemon over TCP.
/// </summary>
public interface IClamAvScanner
{
    /// <summary>
    /// Scans the provided stream without altering its position.
    /// Safe to call with <see cref="Stream.CanSeek"/> = false (implementation buffers internally).
    /// </summary>
    Task<ScanResult> ScanAsync(Stream content, CancellationToken cancellationToken = default);
}
