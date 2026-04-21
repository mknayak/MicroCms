using MicroCMS.Domain.Entities;
using MicroCMS.Shared.Ids;

namespace MicroCMS.Domain.Aggregates.Content;

/// <summary>
/// An immutable snapshot of an entry's field data at a specific point in time.
/// Supports diff-and-rollback (FR-CM-5).
/// </summary>
public sealed class EntryVersion : Entity<Guid>
{
    private EntryVersion() { } // EF Core

    internal EntryVersion(
        EntryId entryId,
        int versionNumber,
        string fieldsJson,
        Guid authorId,
        string? changeNote)
        : base(Guid.NewGuid())
    {
        EntryId = entryId;
        VersionNumber = versionNumber;
        FieldsJson = fieldsJson;
        AuthorId = authorId;
        ChangeNote = changeNote;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public EntryId EntryId { get; private set; }

    /// <summary>1-based monotonically increasing version counter.</summary>
    public int VersionNumber { get; private set; }

    /// <summary>Serialised field key→value map (JSON). Stored as text to remain schema-agnostic.</summary>
    public string FieldsJson { get; private set; } = string.Empty;

    public Guid AuthorId { get; private set; }

    /// <summary>Optional human note describing this version (e.g. "Fixed typo in intro paragraph").</summary>
    public string? ChangeNote { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }
}
