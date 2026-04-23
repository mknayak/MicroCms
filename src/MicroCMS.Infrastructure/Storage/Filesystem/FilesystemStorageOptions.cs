namespace MicroCMS.Infrastructure.Storage.Filesystem;

/// <summary>Configuration for the local-filesystem storage provider.</summary>
public sealed class FilesystemStorageOptions
{
    public const string SectionName = "Storage:Filesystem";

    /// <summary>Absolute path to the root directory where uploaded files are stored.</summary>
    public string RootPath { get; set; } = Path.Combine(Path.GetTempPath(), "MicroCMS", "uploads");
}
