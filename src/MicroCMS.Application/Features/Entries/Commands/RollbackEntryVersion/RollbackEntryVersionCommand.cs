using MicroCMS.Application.Common.Attributes;
using MicroCMS.Application.Common.Authorization;
using MicroCMS.Application.Common.Markers;
using MicroCMS.Application.Features.Entries.Dtos;

namespace MicroCMS.Application.Features.Entries.Commands.RollbackEntryVersion;

/// <summary>
/// Rolls an entry's field data back to a specified historical version.
/// A new version snapshot is automatically created recording the rollback.
/// </summary>
[HasPolicy(ContentPolicies.EntryUpdate)]
public sealed record RollbackEntryVersionCommand(
    Guid EntryId,
    int TargetVersionNumber) : ICommand<EntryDto>;
