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
/// Resolves the available (left-pane) entry list for a <c>MultiList</c> field.
/// Returns entry ID + label pairs, optionally filtered to entries that are members
/// of a specific entry group (when <see cref="FieldDynamicSource.GroupHandle"/> is set).
/// </summary>
[HasPolicy(ContentPolicies.ContentTypeRead)]
public sealed record ResolveMultiListOptionsQuery(
    Guid ContentTypeId,
    Guid FieldId) : IQuery<IReadOnlyList<MultiListOptionDto>>;

/// <summary>An available entry item in the MultiList left pane.</summary>
public sealed record MultiListOptionDto(Guid EntryId, string Label, string Slug);

internal sealed class ResolveMultiListOptionsQueryHandler(
    IRepository<ContentType, ContentTypeId> ctRepo,
    IRepository<Entry, EntryId> entryRepo,
    IRepository<EntryGroup, EntryGroupId> groupRepo,
    ICurrentUser currentUser)
    : IRequestHandler<ResolveMultiListOptionsQuery, Result<IReadOnlyList<MultiListOptionDto>>>
{
    private static readonly JsonSerializerOptions _jsonOpts =
        new() { PropertyNameCaseInsensitive = true };

    public async Task<Result<IReadOnlyList<MultiListOptionDto>>> Handle(
        ResolveMultiListOptionsQuery request,
        CancellationToken cancellationToken)
    {
        if (currentUser.SiteId is not { } siteId)
            return Result.Failure<IReadOnlyList<MultiListOptionDto>>(
                Error.Validation("Auth.NoSiteContext", "No site context in token."));

        var ct = await ctRepo.GetByIdAsync(new ContentTypeId(request.ContentTypeId), cancellationToken)
            ?? throw new NotFoundException(nameof(ContentType), request.ContentTypeId);

        var field = ct.Fields.FirstOrDefault(f => f.Id == request.FieldId)
            ?? throw new NotFoundException("Field", request.FieldId);

        // When the field has no MultiListSource config (e.g. the Groups picker), fall back to
        // loading entries of the owning content type directly.
        var src = field.Validation?.MultiListSource;
        ContentType sourceCt;

        if (src is not null)
        {
            // Resolve explicitly-configured source content type by handle
            var allContentTypes = await ctRepo.ListAsync(
                new ContentTypesBySiteSpec(siteId), cancellationToken);

            sourceCt = allContentTypes.FirstOrDefault(c =>
                c.Handle.Equals(src.ContentTypeHandle, StringComparison.OrdinalIgnoreCase))
                ?? throw new NotFoundException("Source ContentType", src.ContentTypeHandle);
        }
        else
        {
            // No source config: use the owning content type as the implicit source
            sourceCt = ct;
        }

        // Fetch entries (up to 1000 for dual-pane picker)
        var entriesSpec = new EntriesBySiteSpec(
            siteId,
            statusFilter: src?.StatusFilter,
            contentTypeId: sourceCt.Id.Value,
            locale: null,
            folderId: null,
            pageNumber: 1,
            pageSize: 1000);

        var entries = await entryRepo.ListAsync(entriesSpec, cancellationToken);

        // ── Group filter ────────────────────────────────────────────────────
        IEnumerable<Entry> filtered = entries;
        if (!string.IsNullOrWhiteSpace(src?.GroupHandle))
        {
            var groupSpec = new EntryGroupByHandleSpec(siteId, sourceCt.Id, src.GroupHandle);
            var groups = await groupRepo.ListAsync(groupSpec, cancellationToken);
            var memberIds = groups
                .SelectMany(g => g.Members)
                .Select(m => m.EntryId)
                .ToHashSet();

            filtered = entries.Where(e => memberIds.Contains(e.Id));
        }

        var labelField = src?.LabelField ?? string.Empty;
        var options = filtered
            .Select(e => BuildOption(e, labelField))
            .Where(o => o is not null)
            .Select(o => o!)
            .ToList();

        return Result.Success<IReadOnlyList<MultiListOptionDto>>(options);
    }

    private static MultiListOptionDto? BuildOption(Entry entry, string labelField)
    {
        try
        {
            using var doc = JsonDocument.Parse(entry.FieldsJson);
            var root = doc.RootElement;
            var label = TryGetString(root, labelField) ?? entry.Slug.Value;
            return new MultiListOptionDto(entry.Id.Value, label, entry.Slug.Value);
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
