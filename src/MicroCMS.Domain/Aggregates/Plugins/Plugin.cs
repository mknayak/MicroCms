using MicroCMS.Domain.Exceptions;
using MicroCMS.Shared.Ids;

namespace MicroCMS.Domain.Aggregates.Plugins;

/// <summary>Strongly-typed ID for <c>Plugin</c>.</summary>
public readonly record struct PluginId(Guid Value)
{
  public static PluginId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString();
}

/// <summary>
/// Tracks an installed plugin in the tenant's plugin registry (GAP-28/29).
/// Capabilities map to the IPlugin.RequiredCapabilities contract in Plugins.Abstractions.
/// </summary>
public sealed class Plugin : AggregateRoot<PluginId>
{
    private readonly List<string> _capabilities = [];

    private Plugin() : base() { }

  private Plugin(PluginId id, TenantId tenantId, string name, string version, string author)
 : base(id)
    {
   TenantId = tenantId;
      Name = name;
   Version = version;
    Author = author;
    IsActive = true;
  InstalledAt = DateTimeOffset.UtcNow;
    }

    public TenantId TenantId { get; private set; }
 public string Name { get; private set; } = string.Empty;
    public string Version { get; private set; } = string.Empty;
    public string Author { get; private set; } = string.Empty;
    /// <summary>Assembly-level signature for tamper detection.</summary>
    public string? Signature { get; private set; }
    public bool IsActive { get; private set; }
    public DateTimeOffset InstalledAt { get; private set; }
    public IReadOnlyList<string> Capabilities => _capabilities.AsReadOnly();

    public static Plugin Install(
    TenantId tenantId, string name, string version, string author,
 IEnumerable<string> capabilities, string? signature = null)
    {
    ArgumentException.ThrowIfNullOrWhiteSpace(name, nameof(name));
ArgumentException.ThrowIfNullOrWhiteSpace(version, nameof(version));
      ArgumentException.ThrowIfNullOrWhiteSpace(author, nameof(author));

     var plugin = new Plugin(PluginId.New(), tenantId, name, version, author)
     { Signature = signature };
  plugin._capabilities.AddRange(
            capabilities.Select(c => c.Trim()).Where(c => c.Length > 0).Distinct());
 return plugin;
    }

    public void Activate() => IsActive = true;

    public void Deactivate() => IsActive = false;

 public void UpdateVersion(string newVersion, string? newSignature = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(newVersion, nameof(newVersion));
        Version = newVersion;
   Signature = newSignature;
    }
}
