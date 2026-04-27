using System.Text.Json;

namespace MicroCMS.Application.Features.Entries.Dtos;

/// <summary>
/// Represents a historical snapshot of an entry's field data.
/// Returned by the GetEntryVersions query.
/// <c>Fields</c> is exposed as a parsed JSON object (not a raw string).
/// </summary>
public sealed record EntryVersionDto(
    Guid Id,
    Guid EntryId,
    int VersionNumber,
    Dictionary<string, JsonElement>? Fields,
    Guid AuthorId,
    string? ChangeNote,
    DateTimeOffset CreatedAt);
