using MicroCMS.Application.Common.Interfaces;

namespace MicroCMS.Infrastructure.Storage.VirusScan;

/// <summary>
/// Development / test stub that marks every file as clean without connecting to ClamAV.
/// Registered automatically when <c>ClamAv:Enabled</c> is false in configuration.
/// </summary>
public sealed class NoOpClamAvScanner : IClamAvScanner
{
    public Task<ScanResult> ScanAsync(Stream content, CancellationToken cancellationToken = default) =>
        Task.FromResult(new ScanResult(IsClean: true, ThreatName: null));
}
