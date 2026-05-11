namespace MicroCMS.Infrastructure.BackgroundJobs;

/// <summary>
/// Holds the stable identity of this host instance for outbox broadcast tracking.
/// Bind from <c>MicroCMS:Outbox:InstanceId</c>; falls back to <see cref="Environment.MachineName"/>.
/// </summary>
public sealed class OutboxInstanceOptions
{
    public const string SectionName = "MicroCMS:Outbox";

    /// <summary>
    /// Unique identifier for this host process.
    /// Examples: "CM", "CD-01", "CD-02", "Preview".
    /// Must be stable across restarts; used as part of the <c>OutboxDeliveryRecord</c> PK.
    /// </summary>
    public string InstanceId { get; set; } = Environment.MachineName;
}
