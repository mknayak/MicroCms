using System.Text.Json;
using MediatR;
using MicroCMS.Application.Common.Attributes;
using MicroCMS.Application.Common.Authorization;
using MicroCMS.Application.Common.Exceptions;
using MicroCMS.Application.Common.Interfaces;
using MicroCMS.Application.Common.Markers;
using MicroCMS.Domain.Aggregates.Content;
using MicroCMS.Domain.Repositories;
using MicroCMS.Domain.Specifications.Content;
using MicroCMS.Shared.Ids;
using MicroCMS.Shared.Results;

namespace MicroCMS.Application.Features.ContentTypes.Queries;

/// <summary>
/// Resolves the effective option list for an Enum field.
/// <list type="bullet">
///   <item>For <b>static</b> Enum fields: returns the stored options array from ValidationJson.</item>
///   <item>For <b>dynamic</b> Enum fields: queries published entries of the source content type
///    and extracts the label/value pair from the configured fields.</item>
/// </list>
/// Used by the schema designer to preview options, and by the entry editor to populate dropdowns.
/// </summary>
[HasPolicy(ContentPolicies.ContentTypeRead)]
public sealed record ResolveEnumOptionsQuery(
    Guid ContentTypeId,
    Guid FieldId,
    Guid SiteId) : IQuery<IReadOnlyList<EnumOptionDto>>;

public sealed record EnumOptionDto(string Value, string Label);

internal sealed class ResolveEnumOptionsQueryHandler(
    IRepository<ContentType, ContentTypeId> ctRepo,
    IRepository<Entry, EntryId> entryRepo,
    ICurrentUser currentUser)
    : IRequestHandler<ResolveEnumOptionsQuery, Result<IReadOnlyList<EnumOptionDto>>>
{
    private static readonly JsonSerializerOptions _jsonOpts =
        new() { PropertyNameCaseInsensitive = true };

    public async Task<Result<IReadOnlyList<EnumOptionDto>>> Handle(
        ResolveEnumOptionsQuery request,
        CancellationToken cancellationToken)
    {
        var ct = await ctRepo.GetByIdAsync(new ContentTypeId(request.ContentTypeId), cancellationToken)
   ?? throw new NotFoundException(nameof(ContentType), request.ContentTypeId);

        var field = ct.Fields.FirstOrDefault(f => f.Id == request.FieldId)
   ?? throw new NotFoundException("Field", request.FieldId);

        var validation = field.Validation;

        // ── Static options ──────────────────────────────────────────────────
        if (validation?.DynamicSource is null)
        {
            var staticOpts = validation?.Options ?? [];
            return Result.Success<IReadOnlyList<EnumOptionDto>>(
     staticOpts.Select(o => new EnumOptionDto(o, o)).ToList());
        }

        // ── Dynamic options — query published entries of the source type ────
        var src = validation.DynamicSource;
        var siteId = new SiteId(request.SiteId);

        var allContentTypes = await ctRepo.ListAsync(
                new ContentTypesBySiteSpec(siteId), cancellationToken);

        var sourceCt = allContentTypes.FirstOrDefault(c =>
           c.Handle.Equals(src.ContentTypeHandle, StringComparison.OrdinalIgnoreCase))
      ?? throw new NotFoundException("Source ContentType", src.ContentTypeHandle);

        // Fetch published entries of the source type (up to 500 for selects)
        var entriesSpec = new EntriesBySiteSpec(
    siteId,
   statusFilter: src.StatusFilter,
    contentTypeId: sourceCt.Id.Value,
    locale: null,
     folderId: null,
    pageNumber: 1,
      pageSize: 500);

        var entries = await entryRepo.ListAsync(entriesSpec, cancellationToken);

        var options = entries
 .Select(e => BuildOption(e.FieldsJson, src.LabelField, src.ValueField))
         .Where(o => o is not null)
            .Select(o => o!)
   .ToList();

        return Result.Success<IReadOnlyList<EnumOptionDto>>(options);
    }

    private static EnumOptionDto? BuildOption(string fieldsJson, string labelField, string valueField)
    {
        try
        {
            using var doc = JsonDocument.Parse(fieldsJson);
            var root = doc.RootElement;

            var label = TryGetString(root, labelField);
            var value = TryGetString(root, valueField);

            if (string.IsNullOrWhiteSpace(value)) return null;
            return new EnumOptionDto(value, label ?? value);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string? TryGetString(JsonElement root, string key)
    {
        if (root.TryGetProperty(key, out var el) && el.ValueKind == JsonValueKind.String)
            return el.GetString();
        return null;
    }
}
