namespace MicroCMS.Infrastructure.Storage.VirusScan;

/// <summary>Configuration for the ClamAV TCP connection.</summary>
public sealed class ClamAvOptions
{
    public const string SectionName = "ClamAv";

    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 3310;
}
