using MicroCMS.Application.Common.Attributes;
using MicroCMS.Application.Common.Authorization;
using MicroCMS.Application.Common.Markers;
using MicroCMS.Application.Features.Entries.Dtos;

namespace MicroCMS.Application.Features.Entries.Commands.UpdateEntry;

/// <summary>
/// Updates an existing entry's field data and optionally its slug.
/// Only entries in Draft, PendingReview, Approved, Scheduled, or Unpublished status are editable.
/// </summary>
[HasPolicy(ContentPolicies.EntryUpdate)]
public sealed record UpdateEntryCommand(
    Guid EntryId,
    string FieldsJson,
    string? NewSlug = null,
    string? ChangeNote = null) : ICommand<EntryDto>;
