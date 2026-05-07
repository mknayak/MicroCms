namespace MicroCMS.Application.Features.EntryGroups.Dtos;

/// <summary>Full representation of an entry group including its member entry IDs.</summary>
public sealed record EntryGroupDto(
    Guid Id,
    Guid SiteId,
    Guid ContentTypeId,
    string Handle,
    string Title,
    string? Description,
    Guid? ImageAssetId,
    IReadOnlyList<Guid> MemberEntryIds,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

/// <summary>Lightweight list projection — no member IDs, used for group list screens.</summary>
public sealed record EntryGroupListItemDto(
    Guid Id,
    Guid SiteId,
    Guid ContentTypeId,
    string Handle,
    string Title,
    string? Description,
    Guid? ImageAssetId,
    int MemberCount,
    DateTimeOffset UpdatedAt);
