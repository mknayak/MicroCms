using MicroCMS.Application.Common.Attributes;
using MicroCMS.Application.Common.Authorization;
using MicroCMS.Application.Common.Markers;
using MicroCMS.Application.Features.Entries.Dtos;

namespace MicroCMS.Application.Features.Entries.Commands.UpdateSeoMetadata;

/// <summary>
/// Updates the SEO/SERP metadata fields on an entry (GAP-08).
/// All fields are optional; null values clear existing data.
/// </summary>
[HasPolicy(ContentPolicies.EntryUpdate)]
public sealed record UpdateSeoMetadataCommand(
    Guid EntryId,
    string? MetaTitle,
  string? MetaDescription,
    string? CanonicalUrl = null,
    string? OgImage = null) : ICommand<EntryDto>;
