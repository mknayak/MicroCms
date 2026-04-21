using MicroCMS.Domain.Exceptions;

namespace MicroCMS.Domain.ValueObjects;

/// <summary>
/// Immutable metadata snapshot for a media asset.
/// Captures file characteristics at upload time.
/// </summary>
public sealed class AssetMetadata : ValueObject
{
    public const int MaxFileNameLength = 255;
    public const int MaxMimeTypeLength = 127;
    public const long MaxFileSizeBytes = 2L * 1024 * 1024 * 1024; // 2 GB (FR-ML-1)

    private AssetMetadata(
        string fileName,
        string mimeType,
        long sizeBytes,
        int? widthPx,
        int? heightPx,
        TimeSpan? duration,
        IReadOnlyDictionary<string, string> exifData)
    {
        FileName = fileName;
        MimeType = mimeType;
        SizeBytes = sizeBytes;
        WidthPx = widthPx;
        HeightPx = heightPx;
        Duration = duration;
        ExifData = exifData;
    }

    public string FileName { get; }
    public string MimeType { get; }
    public long SizeBytes { get; }
    public int? WidthPx { get; }
    public int? HeightPx { get; }
    public TimeSpan? Duration { get; }
    public IReadOnlyDictionary<string, string> ExifData { get; }

    public bool IsImage => MimeType.StartsWith("image/", StringComparison.OrdinalIgnoreCase);
    public bool IsVideo => MimeType.StartsWith("video/", StringComparison.OrdinalIgnoreCase);
    public bool IsAudio => MimeType.StartsWith("audio/", StringComparison.OrdinalIgnoreCase);

    public static AssetMetadata Create(
        string fileName,
        string mimeType,
        long sizeBytes,
        int? widthPx = null,
        int? heightPx = null,
        TimeSpan? duration = null,
        IReadOnlyDictionary<string, string>? exifData = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName, nameof(fileName));
        ArgumentException.ThrowIfNullOrWhiteSpace(mimeType, nameof(mimeType));

        if (fileName.Length > MaxFileNameLength)
        {
            throw new DomainException($"File name must not exceed {MaxFileNameLength} characters.");
        }

        if (mimeType.Length > MaxMimeTypeLength)
        {
            throw new DomainException($"MIME type must not exceed {MaxMimeTypeLength} characters.");
        }

        if (sizeBytes <= 0 || sizeBytes > MaxFileSizeBytes)
        {
            throw new DomainException(
                $"File size must be between 1 byte and {MaxFileSizeBytes / (1024 * 1024 * 1024)} GB.");
        }

        return new AssetMetadata(
            fileName.Trim(),
            mimeType.Trim().ToLowerInvariant(),
            sizeBytes,
            widthPx,
            heightPx,
            duration,
            exifData ?? new Dictionary<string, string>());
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return FileName;
        yield return MimeType;
        yield return SizeBytes;
        yield return WidthPx;
        yield return HeightPx;
        yield return Duration;
    }
}
