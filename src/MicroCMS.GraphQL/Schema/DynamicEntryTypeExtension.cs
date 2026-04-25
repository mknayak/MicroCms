using HotChocolate;
using HotChocolate.Types;
using MicroCMS.Application.Features.Entries.Dtos;

namespace MicroCMS.GraphQL.Schema;

/// <summary>
/// Hot Chocolate type extension that adds convenience fields to <see cref="EntryDto"/>
/// at the GraphQL schema level without modifying the Application DTO.
///
/// Adds:
/// - <c>hasPublishedVersion</c> — computed from <see cref="EntryDto.PublishedAt"/>.
/// - <c>isScheduled</c>        — computed from <see cref="EntryDto.ScheduledPublishAt"/>.
/// - <c>fieldsJson</c>         — re-exposed under the conventional GraphQL field name.
/// </summary>
[ExtendObjectType(typeof(EntryDto))]
public sealed class DynamicEntryTypeExtension
{
    /// <summary>Whether the entry has ever been published (PublishedAt is set).</summary>
    public bool HasPublishedVersion([Parent] EntryDto entry) =>
        entry.PublishedAt.HasValue;

    /// <summary>Whether the entry is awaiting a scheduled publish.</summary>
    public bool IsScheduled([Parent] EntryDto entry) =>
     entry.ScheduledPublishAt.HasValue && !entry.PublishedAt.HasValue;
}
