namespace MicroCMS.Plugins.Abstractions.Manifests;

/// <summary>
/// Deserialized from <c>plugin.json</c> bundled with each plugin package.
/// The host validates the manifest before loading the assembly.
/// </summary>
public sealed class PluginManifest
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Version { get; init; } = string.Empty;
    public string EntryAssembly { get; init; } = string.Empty;
    public string PluginTypeName { get; init; } = string.Empty;
    public IReadOnlyList<string> RequiredCapabilities { get; init; } = [];
    public string? PublicKeyToken { get; init; }
    public string MinHostVersion { get; init; } = string.Empty;
}
