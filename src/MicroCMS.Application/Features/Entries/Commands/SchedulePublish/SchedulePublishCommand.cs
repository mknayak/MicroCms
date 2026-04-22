using MicroCMS.Application.Common.Attributes;
using MicroCMS.Application.Common.Authorization;
using MicroCMS.Application.Common.Markers;
using MicroCMS.Application.Features.Entries.Dtos;

namespace MicroCMS.Application.Features.Entries.Commands.SchedulePublish;

/// <summary>
/// Schedules an Approved entry for future publication.
/// If <see cref="UnpublishAt"/> is provided it must be after <see cref="PublishAt"/>.
/// </summary>
[HasPolicy(ContentPolicies.EntrySchedule)]
public sealed record SchedulePublishCommand(
    Guid EntryId,
    DateTimeOffset PublishAt,
    DateTimeOffset? UnpublishAt = null) : ICommand<EntryDto>;
