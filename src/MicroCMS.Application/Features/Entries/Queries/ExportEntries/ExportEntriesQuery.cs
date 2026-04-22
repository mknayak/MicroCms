using MicroCMS.Application.Common.Attributes;
using MicroCMS.Application.Common.Authorization;
using MicroCMS.Application.Common.Markers;

namespace MicroCMS.Application.Features.Entries.Queries.ExportEntries;

/// <summary>
/// Exports all entries for a site/content-type as a structured byte stream (GAP-05).
/// Format is controlled by <see cref="ExportFormat"/>. The result is intended to be
/// written directly to an HTTP response via content-type negotiation.
/// </summary>
[HasPolicy(ContentPolicies.EntryExport)]
public sealed record ExportEntriesQuery(
    Guid SiteId,
    Guid? ContentTypeId = null,
    ExportFormat Format = ExportFormat.Json) : IQuery<ExportResult>;

public enum ExportFormat
{
  Json = 0,
    Csv = 1
}

/// <summary>Contains the exported bytes and the appropriate MIME type.</summary>
public sealed record ExportResult(byte[] Data, string ContentType, string FileName);
