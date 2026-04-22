using MicroCMS.Application.Common.Attributes;
using MicroCMS.Application.Common.Authorization;
using MicroCMS.Application.Common.Markers;
using MicroCMS.Application.Features.Entries.Dtos;

namespace MicroCMS.Application.Features.Entries.Commands.CancelScheduledPublish;

/// <summary>
/// Cancels a pending scheduled publish, returning the entry to Approved status (GAP-06).
/// The entry's PublishAt and UnpublishAt timestamps are cleared.
/// </summary>
[HasPolicy(ContentPolicies.EntrySchedule)]
public sealed record CancelScheduledPublishCommand(Guid EntryId) : ICommand<EntryDto>;
