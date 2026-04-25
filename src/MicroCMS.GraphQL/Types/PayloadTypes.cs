using MicroCMS.Application.Features.ContentTypes.Dtos;
using MicroCMS.Application.Features.Entries.Dtos;

namespace MicroCMS.GraphQL.Types;

// ── Entry payload ──────────────────────────────────────────────────────────

/// <summary>Mutation response for entry operations.</summary>
public sealed class EntryPayload
{
    public EntryDto? Entry { get; init; }
    public string? Error { get; init; }
    public bool IsSuccess => Error is null;

  public static EntryPayload Ok(EntryDto entry) => new() { Entry = entry };
    public static EntryPayload Fail(string error) => new() { Error = error };
}

// ── ContentType payload ────────────────────────────────────────────────────

/// <summary>Mutation response for content type operations.</summary>
public sealed class ContentTypePayload
{
    public ContentTypeDto? ContentType { get; init; }
    public string? Error { get; init; }
    public bool IsSuccess => Error is null;

    public static ContentTypePayload Ok(ContentTypeDto ct) => new() { ContentType = ct };
    public static ContentTypePayload Fail(string error) => new() { Error = error };
}

// ── Delete payload ─────────────────────────────────────────────────────────

/// <summary>Mutation response for delete operations.</summary>
public sealed class DeletePayload
{
    public Guid? DeletedId { get; init; }
    public string? Error { get; init; }
    public bool IsSuccess => Error is null;

    public static DeletePayload Ok(Guid id) => new() { DeletedId = id };
    public static DeletePayload Fail(string error) => new() { Error = error };
}
