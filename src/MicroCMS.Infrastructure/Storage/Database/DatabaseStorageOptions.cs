namespace MicroCMS.Infrastructure.Storage.Database;

/// <summary>Configuration for the database-backed storage provider.</summary>
public sealed class DatabaseStorageOptions
{
    public const string SectionName = "Storage:Database";

    /// <summary>
    /// Path used as the local read-through cache.
    /// Relative paths are resolved from the application's content root
    /// (e.g. <c>App_Data\media-cache</c> → <c>{ContentRoot}\App_Data\media-cache</c>).
    /// Absolute paths are used as-is.
    /// Defaults to <c>App_Data\media-cache</c> relative to the content root.
    /// </summary>
    public string CachePath { get; set; } = @"App_Data\media-cache";

    /// <summary>
    /// Maximum file size in bytes to cache on the local filesystem.
    /// Files larger than this are always streamed directly from the database.
    /// Default: 50 MB.
    /// </summary>
    public long MaxCachedFileSizeBytes { get; set; } = 50L * 1024 * 1024;
}
