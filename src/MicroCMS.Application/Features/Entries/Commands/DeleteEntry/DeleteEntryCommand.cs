using MicroCMS.Application.Common.Attributes;
using MicroCMS.Application.Common.Authorization;
using MicroCMS.Application.Common.Markers;

namespace MicroCMS.Application.Features.Entries.Commands.DeleteEntry;

/// <summary>
/// Permanently removes an entry and all its versions.
/// Published entries must be unpublished before deletion (enforced by the domain).
/// </summary>
[HasPolicy(ContentPolicies.EntryDelete)]
public sealed record DeleteEntryCommand(Guid EntryId) : ICommand;
