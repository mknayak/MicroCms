using MicroCMS.Application.Common.Attributes;
using MicroCMS.Application.Common.Authorization;
using MicroCMS.Application.Common.Markers;

namespace MicroCMS.Application.Features.Entries.Commands.ImportEntries;

/// <summary>
/// Imports entries from a ZIP file that contains an <c>entries.json</c> file.
/// Each record in the JSON array is upserted as a new Draft entry (existing slugs are skipped).
/// </summary>
[HasPolicy(ContentPolicies.EntryImport)]
public sealed record ImportEntriesCommand(byte[] ZipBytes) : ICommand<ImportEntriesResult>;

/// <summary>Summary returned after a completed import.</summary>
public sealed record ImportEntriesResult(int Imported, int Skipped, IReadOnlyList<string> Errors);
