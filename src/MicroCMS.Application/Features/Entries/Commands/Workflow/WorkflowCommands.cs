using MicroCMS.Application.Common.Attributes;
using MicroCMS.Application.Common.Authorization;
using MicroCMS.Application.Common.Markers;
using MicroCMS.Application.Features.Entries.Dtos;

namespace MicroCMS.Application.Features.Entries.Commands.Workflow;

/// <summary>Submits a Draft entry for review (GAP-07). Transitions Draft → PendingReview.</summary>
[HasPolicy(ContentPolicies.EntryUpdate)]
public sealed record SubmitForReviewCommand(Guid EntryId) : ICommand<EntryDto>;

/// <summary>Approves an entry that is PendingReview (GAP-07). Requires Approver or above.</summary>
[HasPolicy(ContentPolicies.EntryReview)]
public sealed record ApproveEntryCommand(Guid EntryId) : ICommand<EntryDto>;

/// <summary>
/// Rejects a PendingReview entry and returns it to Draft with a required reason (GAP-07).
/// Requires Approver or above.
/// </summary>
[HasPolicy(ContentPolicies.EntryReview)]
public sealed record RejectEntryCommand(Guid EntryId, string Reason) : ICommand<EntryDto>;
