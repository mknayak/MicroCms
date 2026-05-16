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

        var src = field.Validation?.MultiListSource;
        var sourceCt = await ResolveSourceContentTypeAsync(ct, src, siteId, cancellationToken);
        var entries = await FetchEntriesAsync(sourceCt, src, siteId, cancellationToken);
        var filtered = await ApplyGroupFilterAsync(entries, src, sourceCt, siteId, cancellationToken);

        var labelField = src?.LabelField ?? string.Empty;
        var options = filtered
            .Select(e => BuildOption(e, labelField))
            .Where(o => o is not null)
            .Select(o => o!)
            .ToList();

        return Result.Success<IReadOnlyList<MultiListOptionDto>>(options);
    }

    private async Task<ContentType> ResolveSourceContentTypeAsync(
        ContentType owningCt,
        FieldDynamicSource? src,
        SiteId siteId,
        CancellationToken cancellationToken)
    {
        if (src is null)
            return owningCt;

        var allContentTypes = await ctRepo.ListAsync(
            new ContentTypesBySiteSpec(siteId), cancellationToken);

        return allContentTypes.FirstOrDefault(c =>
            c.Handle.Equals(src.ContentTypeHandle, StringComparison.OrdinalIgnoreCase))
            ?? throw new NotFoundException("Source ContentType", src.ContentTypeHandle);
    }

    private async Task<IReadOnlyList<Entry>> FetchEntriesAsync(
        ContentType sourceCt,
        FieldDynamicSource? src,
        SiteId siteId,
        CancellationToken cancellationToken)
    {
        var spec = new EntriesBySiteSpec(
            siteId,
            statusFilter: src?.StatusFilter,
            contentTypeId: sourceCt.Id.Value,
            locale: null,
            folderId: null,
            pageNumber: 1,
            pageSize: 1000);

        return await entryRepo.ListAsync(spec, cancellationToken);
    }

    private async Task<IEnumerable<Entry>> ApplyGroupFilterAsync(
        IReadOnlyList<Entry> entries,
        FieldDynamicSource? src,
        ContentType sourceCt,
        SiteId siteId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(src?.GroupHandle))
            return entries;

        var groupSpec = new EntryGroupByHandleSpec(siteId, sourceCt.Id, src.GroupHandle);
        var groups = await groupRepo.ListAsync(groupSpec, cancellationToken);
        var memberIds = groups
            .SelectMany(g => g.Members)
            .Select(m => m.EntryId)
            .ToHashSet();

        return entries.Where(e => memberIds.Contains(e.Id));
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

/// <summary>
/// Resolves the available (left-pane) entry list for a <c>MultiList</c> field directly
/// from source parameters — no owning content-type/field lookup required.
/// Used when the caller has a <c>multiListSource</c> config but no content-type + field ID pair
/// (e.g. the component-item editor).
/// </summary>
[HasPolicy(ContentPolicies.ContentTypeRead)]
public sealed record ResolveMultiListOptionsBySourceQuery(
    string SourceContentTypeHandle,
    string LabelField,
    string? StatusFilter = null,
    string? GroupHandle = null) : IQuery<IReadOnlyList<MultiListOptionDto>>;

internal sealed class ResolveMultiListOptionsBySourceQueryHandler(
    IRepository<ContentType, ContentTypeId> ctRepo,
    IRepository<Entry, EntryId> entryRepo,
    IRepository<EntryGroup, EntryGroupId> groupRepo,
    ICurrentUser currentUser)
    : IRequestHandler<ResolveMultiListOptionsBySourceQuery, Result<IReadOnlyList<MultiListOptionDto>>>
{
    private static readonly JsonSerializerOptions _jsonOpts =
        new() { PropertyNameCaseInsensitive = true };

    public async Task<Result<IReadOnlyList<MultiListOptionDto>>> Handle(
        ResolveMultiListOptionsBySourceQuery request,
        CancellationToken cancellationToken)
    {
        if (currentUser.SiteId is not { } siteId)
            return Result.Failure<IReadOnlyList<MultiListOptionDto>>(
                Error.Validation("Auth.NoSiteContext", "No site context in token."));

        var allTypes = await ctRepo.ListAsync(new ContentTypesBySiteSpec(siteId), cancellationToken);
        var sourceCt = allTypes.FirstOrDefault(c =>
            c.Handle.Equals(request.SourceContentTypeHandle, StringComparison.OrdinalIgnoreCase))
            ?? throw new NotFoundException("Source ContentType", request.SourceContentTypeHandle);

        var spec = new EntriesBySiteSpec(
            siteId,
            statusFilter: request.StatusFilter,
            contentTypeId: sourceCt.Id.Value,
            locale: null,
            folderId: null,
            pageNumber: 1,
            pageSize: 1000);

        var entries = await entryRepo.ListAsync(spec, cancellationToken);

        IEnumerable<Entry> filtered = entries;
        if (!string.IsNullOrWhiteSpace(request.GroupHandle))
        {
            var groupSpec = new EntryGroupByHandleSpec(siteId, sourceCt.Id, request.GroupHandle);
            var groups = await groupRepo.ListAsync(groupSpec, cancellationToken);
            var memberIds = groups
                .SelectMany(g => g.Members)
                .Select(m => m.EntryId)
                .ToHashSet();
            filtered = entries.Where(e => memberIds.Contains(e.Id));
        }

        var options = filtered
            .Select(e => BuildOption(e, request.LabelField))
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
        if (string.IsNullOrWhiteSpace(key)) return null;
        if (root.TryGetProperty(key, out var el) && el.ValueKind == JsonValueKind.String)
            return el.GetString();
        return null;
    }
}
