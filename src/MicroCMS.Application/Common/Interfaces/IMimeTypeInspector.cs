namespace MicroCMS.Application.Common.Interfaces;

/// <summary>
/// Detects the true MIME type of a byte stream by reading its magic-byte header,
/// independent of the file extension provided by the upload client.
/// </summary>
public interface IMimeTypeInspector
{
    /// <summary>
    /// Reads up to the first 512 bytes of <paramref name="content"/> to identify the MIME type.
    /// The stream position is reset to where it started after the probe.
    /// Falls back to the extension of <paramref name="fallbackFileName"/> when no magic bytes match.
    /// </summary>
    Task<string> DetectAsync(
        Stream content,
        string fallbackFileName,
        CancellationToken cancellationToken = default);
}
