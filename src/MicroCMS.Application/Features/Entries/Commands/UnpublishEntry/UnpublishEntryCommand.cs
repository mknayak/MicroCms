using MicroCMS.Application.Common.Attributes;
using MicroCMS.Application.Common.Authorization;
using MicroCMS.Application.Common.Markers;
using MicroCMS.Application.Features.Entries.Dtos;

namespace MicroCMS.Application.Features.Entries.Commands.UnpublishEntry;

/// <summary>Moves a Published entry back to Unpublished status.</summary>
[HasPolicy(ContentPolicies.EntryPublish)]
public sealed record UnpublishEntryCommand(Guid EntryId) : ICommand<EntryDto>;
