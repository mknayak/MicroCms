using Microsoft.Extensions.DependencyInjection;

namespace MicroCMS.Plugins.Abstractions.Contracts;

/// <summary>
/// Root contract that every MicroCMS plugin must implement.
/// The host discovers this interface via reflection inside an <c>AssemblyLoadContext</c>.
/// </summary>
public interface IPlugin
{
    /// <summary>Unique reverse-DNS identifier, e.g. "com.acme.image-optimiser".</summary>
    string Id { get; }

    /// <summary>Human-readable display name.</summary>
    string Name { get; }

    /// <summary>Semantic version string, e.g. "1.2.3".</summary>
    string Version { get; }

    /// <summary>Capabilities this plugin requires from the host (e.g. "storage", "webhooks").</summary>
    IReadOnlyList<string> RequiredCapabilities { get; }

    /// <summary>Register the plugin's services into the host DI container.</summary>
    void ConfigureServices(IServiceCollection services);
}
