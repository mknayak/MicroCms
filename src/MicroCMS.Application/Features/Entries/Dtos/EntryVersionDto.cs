namespace MicroCMS.Application.Features.Entries.Dtos;

/// <summary>
/// Represents a historical snapshot of an entry's field data.
/// Returned by the GetEntryVersions query.
/// </summary>
public sealed record EntryVersionDto(
    Guid Id,
    Guid EntryId,
    int VersionNumber,
    string FieldsJson,
    Guid AuthorId,
    string? ChangeNote,
    DateTimeOffset CreatedAt);
