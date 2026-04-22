using System.Text;
using System.Text.Json;
using MediatR;
using MicroCMS.Domain.Aggregates.Content;
using MicroCMS.Domain.Repositories;
using MicroCMS.Domain.Specifications.Content;
using MicroCMS.Shared.Ids;
using MicroCMS.Shared.Results;

namespace MicroCMS.Application.Features.Entries.Queries.ExportEntries;

/// <summary>Handles <see cref="ExportEntriesQuery"/>.</summary>
public sealed class ExportEntriesQueryHandler(IRepository<Entry, EntryId> entryRepository)
    : IRequestHandler<ExportEntriesQuery, Result<ExportResult>>
{
    public async Task<Result<ExportResult>> Handle(
 ExportEntriesQuery request,
     CancellationToken cancellationToken)
    {
   var siteId = new SiteId(request.SiteId);
     var spec = new EntriesBySiteSpec(siteId, statusFilter: null);
        var entries = await entryRepository.ListAsync(spec, cancellationToken);

        if (request.ContentTypeId.HasValue)
        {
     var ctId = new ContentTypeId(request.ContentTypeId.Value);
   entries = entries.Where(e => e.ContentTypeId == ctId).ToList();
        }

      var result = request.Format == ExportFormat.Csv
   ? BuildCsv(entries)
      : BuildJson(entries);

      return Result.Success(result);
    }

private static ExportResult BuildJson(IEnumerable<Entry> entries)
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
  var bytes = Encoding.UTF8.GetBytes(json);
   return new ExportResult(bytes, "application/json", "entries.json");
    }

    private static ExportResult BuildCsv(IEnumerable<Entry> entries)
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

    return new ExportResult(Encoding.UTF8.GetBytes(sb.ToString()), "text/csv", "entries.csv");
    }

    private static string EscapeCsv(string value) =>
        value.Contains(',') || value.Contains('"') || value.Contains('\n')
            ? $"\"{value.Replace("\"", "\"\"")}\""
          : value;
}
