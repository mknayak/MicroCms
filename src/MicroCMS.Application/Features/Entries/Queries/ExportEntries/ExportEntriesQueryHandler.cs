using System.IO.Compression;
using System.Text;
using System.Text.Json;
using MediatR;
using MicroCMS.Application.Common.Interfaces;
using MicroCMS.Domain.Aggregates.Content;
using MicroCMS.Domain.Repositories;
using MicroCMS.Domain.Specifications.Content;
using MicroCMS.Shared.Ids;
using MicroCMS.Shared.Results;

namespace MicroCMS.Application.Features.Entries.Queries.ExportEntries;

/// <summary>Handles <see cref="ExportEntriesQuery"/>.</summary>
public sealed class ExportEntriesQueryHandler(
    IRepository<Entry, EntryId> entryRepository,
    ICurrentUser currentUser)
    : IRequestHandler<ExportEntriesQuery, Result<ExportResult>>
{
    public async Task<Result<ExportResult>> Handle(
        ExportEntriesQuery request,
        CancellationToken cancellationToken)
    {
        if (currentUser.SiteId is not { } siteId)
            return Result.Failure<ExportResult>(Error.Validation("Auth.NoSiteContext", "No site context in token. Call POST /auth/switch-site first."));

        var spec = new EntriesBySiteSpec(siteId, statusFilter: null);
        var entries = await entryRepository.ListAsync(spec, cancellationToken);

        if (request.ContentTypeId.HasValue)
        {
            var ctId = new ContentTypeId(request.ContentTypeId.Value);
            entries = entries.Where(e => e.ContentTypeId == ctId).ToList();
        }

        var result = request.Format == ExportFormat.Csv
            ? BuildCsvZip(entries)
            : BuildJsonZip(entries);

        return Result.Success(result);
    }

    private static ExportResult BuildJsonZip(IEnumerable<Entry> entries)
    {
        var records = entries.Select(e => new
        {
            id = e.Id.Value,
            siteId = e.SiteId.Value,
            contentTypeId = e.ContentTypeId.Value,
            slug = e.Slug.Value,
            locale = e.Locale.Value,
            status = e.Status.ToString(),
            fieldsJson = e.FieldsJson,
            publishedAt = e.PublishedAt
        });

        var json = JsonSerializer.Serialize(records, new JsonSerializerOptions { WriteIndented = true });
        var jsonBytes = Encoding.UTF8.GetBytes(json);

        var zipBytes = CreateZip("entries.json", jsonBytes);
        return new ExportResult(zipBytes, "application/zip", "entries.zip");
    }

    private static ExportResult BuildCsvZip(IEnumerable<Entry> entries)
    {
        var sb = new StringBuilder();
        sb.AppendLine("id,siteId,contentTypeId,slug,locale,status,publishedAt");

        foreach (var e in entries)
        {
            sb.Append(e.Id.Value).Append(',')
              .Append(e.SiteId.Value).Append(',')
              .Append(e.ContentTypeId.Value).Append(',')
              .Append(EscapeCsv(e.Slug.Value)).Append(',')
              .Append(EscapeCsv(e.Locale.Value)).Append(',')
              .Append(e.Status).Append(',')
              .AppendLine(e.PublishedAt?.ToString("O") ?? string.Empty);
        }

        var csvBytes = Encoding.UTF8.GetBytes(sb.ToString());
        var zipBytes = CreateZip("entries.csv", csvBytes);
        return new ExportResult(zipBytes, "application/zip", "entries.zip");
    }

    private static byte[] CreateZip(string entryFileName, byte[] fileBytes)
    {
        using var ms = new MemoryStream();
        using (var archive = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true))
        {
            var zipEntry = archive.CreateEntry(entryFileName, CompressionLevel.Optimal);
            using var entryStream = zipEntry.Open();
            entryStream.Write(fileBytes, 0, fileBytes.Length);
        }
        return ms.ToArray();
    }

    private static string EscapeCsv(string value) =>
        value.Contains(',') || value.Contains('"') || value.Contains('\n')
            ? $"\"{value.Replace("\"", "\"\"")}\""
            : value;
}

