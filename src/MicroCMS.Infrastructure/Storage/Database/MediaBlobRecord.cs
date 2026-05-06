using MicroCMS.Infrastructure.Persistence.Common;
using Microsoft.EntityFrameworkCore;

namespace MicroCMS.Infrastructure.Storage.Database;

/// <summary>
/// Represents a raw binary blob stored in the database.
/// Keyed by the same opaque storage key used by other <c>IStorageProvider</c> implementations.
/// </summary>
public sealed class MediaBlobRecord
{
    /// <summary>Opaque storage key (e.g. <c>tenantId/2024/06/guid_photo.svg</c>).</summary>
    public string StorageKey { get; private set; } = default!;

    public string MimeType { get; private set; } = default!;

    /// <summary>Raw file bytes.</summary>
    public byte[] Data { get; private set; } = default!;

    public DateTimeOffset CreatedAt { get; private set; }

    private MediaBlobRecord() { }

    public static MediaBlobRecord Create(string storageKey, string mimeType, byte[] data) =>
        new()
        {
            StorageKey = storageKey,
            MimeType = mimeType,
            Data = data,
            CreatedAt = DateTimeOffset.UtcNow,
        };
}
