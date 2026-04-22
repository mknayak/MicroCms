using MicroCMS.Application.Common.Attributes;
using MicroCMS.Application.Common.Authorization;
using MicroCMS.Application.Common.Markers;
using MicroCMS.Application.Features.Entries.Dtos;

namespace MicroCMS.Application.Features.Entries.Commands.PublishEntry;

/// <summary>Publishes an Approved or Scheduled entry immediately.</summary>
[HasPolicy(ContentPolicies.EntryPublish)]
public sealed record PublishEntryCommand(Guid EntryId) : ICommand<EntryDto>;
