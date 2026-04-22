using MicroCMS.Application.Common.Attributes;
using MicroCMS.Application.Common.Authorization;
using MicroCMS.Application.Common.Markers;
using MicroCMS.Application.Features.Entries.Dtos;

namespace MicroCMS.Application.Features.Entries.Queries.GetEntry;

/// <summary>Returns the full DTO for a single entry by ID.</summary>
[HasPolicy(ContentPolicies.EntryRead)]
public sealed record GetEntryQuery(Guid EntryId) : IQuery<EntryDto>;
