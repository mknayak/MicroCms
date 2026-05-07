using System.IO.Compression;
using System.Text.Json;
using MediatR;
using MicroCMS.Application.Common.Interfaces;
using MicroCMS.Domain.Aggregates.Content;
using MicroCMS.Domain.Repositories;
using MicroCMS.Domain.Specifications.Content;
using MicroCMS.Domain.ValueObjects;
using MicroCMS.Shared.Ids;
using MicroCMS.Shared.Results;

namespace MicroCMS.Application.Features.Entries.Commands.ImportEntries;

/// <summary>Handles <see cref="ImportEntriesCommand"/>.</summary>
public sealed class ImportEntriesCommandHandler(
    IRepository<Entry, EntryId> entryRepository,
    ICurrentUser currentUser)
    : IRequestHandler<ImportEntriesCommand, Result<ImportEntriesResult>>
{
    private static readonly JsonSerializerOptions _jsonOpts = new() { PropertyNameCaseInsensitive = true };

    public async Task<Result<ImportEntriesResult>> Handle(
        ImportEntriesCommand request,
        CancellationToken cancellationToken)
    {
        if (currentUser.SiteId is not { } siteId)
            return Result.Failure<ImportEntriesResult>(Error.Validation("Auth.NoSiteContext", "No site context in token. Call POST /auth/switch-site first."));

        // Extract entries.json from the ZIP
        List<ImportEntryRecord> records;
        try
        {
            records = ReadEntriesFromZip(request.ZipBytes);
        }
        catch (Exception ex)
        {
            return Result.Failure<ImportEntriesResult>(
                Error.Validation("Entry.Import.InvalidZip", $"Could not read ZIP: {ex.Message}"));
        }

        int imported = 0, skipped = 0;
        var errors = new List<string>();

        foreach (var record in records)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(record.Slug) || string.IsNullOrWhiteSpace(record.Locale))
                {
                    errors.Add($"Skipped record with missing slug or locale.");
                    skipped++;
                    continue;
                }

                // Skip if slug already exists for this site+locale
                var spec = new EntryBySlugAndSiteSpec(siteId, record.Slug, record.Locale);
                var existing = await entryRepository.ListAsync(spec, cancellationToken);
                if (existing.Count > 0)
                {
                    skipped++;
                    continue;
                }

                var contentTypeId = new ContentTypeId(record.ContentTypeId);
                var slug = Slug.Create(record.Slug);
                var locale = Locale.Create(record.Locale);
                var fieldsJson = record.FieldsJson ?? "{}";

                var entry = Entry.Create(
                    tenantId: currentUser.TenantId,
                    siteId: siteId,
                    contentTypeId: contentTypeId,
                    slug: slug,
                    locale: locale,
                    authorId: currentUser.UserId,
                    fieldsJson: fieldsJson);

                await entryRepository.AddAsync(entry, cancellationToken);
                imported++;
            }
            catch (Exception ex)
            {
                errors.Add($"Failed to import '{record.Slug}': {ex.Message}");
                skipped++;
            }
        }

        return Result.Success(new ImportEntriesResult(imported, skipped, errors));
    }

    private static List<ImportEntryRecord> ReadEntriesFromZip(byte[] zipBytes)
    {
        using var ms = new MemoryStream(zipBytes);
        using var archive = new ZipArchive(ms, ZipArchiveMode.Read);

        var jsonEntry = archive.Entries.FirstOrDefault(e =>
            e.Name.EndsWith(".json", StringComparison.OrdinalIgnoreCase));

        if (jsonEntry is null)
            throw new InvalidOperationException("No .json file found inside the ZIP archive.");

        using var stream = jsonEntry.Open();
        using var reader = new StreamReader(stream);
        var json = reader.ReadToEnd();

        return JsonSerializer.Deserialize<List<ImportEntryRecord>>(json, _jsonOpts)
            ?? throw new InvalidOperationException("JSON file is empty or invalid.");
    }
}

/// <summary>Matches the shape produced by <see cref="ExportEntriesQueryHandler"/>.</summary>
internal sealed class ImportEntryRecord
{
    public Guid Id { get; set; }
    public Guid SiteId { get; set; }
    public Guid ContentTypeId { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string Locale { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? FieldsJson { get; set; }
    public DateTimeOffset? PublishedAt { get; set; }
}
