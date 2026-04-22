using MicroCMS.Application.Common.Attributes;
using MicroCMS.Application.Common.Authorization;
using MicroCMS.Application.Common.Markers;

namespace MicroCMS.Application.Features.Entries.Commands.Bulk;

/// <summary>
/// Publishes multiple approved entries in one operation (GAP-04).
/// Returns a summary of successes and failures.
/// </summary>
[HasPolicy(ContentPolicies.EntryPublish)]
public sealed record BulkPublishEntriesCommand(IReadOnlyList<Guid> EntryIds) : ICommand<BulkOperationResult>;

/// <summary>Unpublishes multiple published entries (GAP-04).</summary>
[HasPolicy(ContentPolicies.EntryPublish)]
public sealed record BulkUnpublishEntriesCommand(IReadOnlyList<Guid> EntryIds) : ICommand<BulkOperationResult>;

/// <summary>
/// Permanently deletes multiple non-published entries (GAP-04).
/// Published entries in the list are skipped and reported as failures.
/// </summary>
[HasPolicy(ContentPolicies.EntryDelete)]
public sealed record BulkDeleteEntriesCommand(IReadOnlyList<Guid> EntryIds) : ICommand<BulkOperationResult>;

/// <summary>
/// Summary DTO returned by all bulk operations.
/// Lists successful and failed IDs so callers can present per-row feedback.
/// </summary>
public sealed record BulkOperationResult(
    IReadOnlyList<Guid> Succeeded,
  IReadOnlyList<BulkOperationFailure> Failed)
{
    public bool HasFailures => Failed.Count > 0;
}

public sealed record BulkOperationFailure(Guid EntryId, string Reason);
