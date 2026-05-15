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
/// Resolves the selectable options for a dynamic <c>Enum</c> field by querying
/// published (or draft) entries from the configured source content type.
/// Returns label + value pairs using the field's <c>LabelField</c> and <c>ValueField</c>.
/// </summary>
[HasPolicy(ContentPolicies.ContentTypeRead)]
public sealed record ResolveDynamicEnumOptionsQuery(
    Guid ContentTypeId,
    Guid FieldId) : IQuery<IReadOnlyList<DynamicEnumOptionDto>>;

/// <summary>
/// Resolves dynamic enum options directly from source parameters (no owning content-type/field lookup).
/// Used when the caller has a <c>dynamicSource</c> config but no content-type + field ID pair
/// (e.g. the component-item editor).
/// </summary>
[HasPolicy(ContentPolicies.ContentTypeRead)]
public sealed record ResolveDynamicEnumOptionsBySourceQuery(
    string SourceContentTypeHandle,
    string LabelField,
    string ValueField,
    string? StatusFilter = null) : IQuery<IReadOnlyList<DynamicEnumOptionDto>>;

/// <summary>A single option in a dynamic enum dropdown.</summary>
public sealed record DynamicEnumOptionDto(string Label, string Value);

internal sealed class ResolveDynamicEnumOptionsQueryHandler(
    IRepository<ContentType, ContentTypeId> ctRepo,
    IRepository<Entry, EntryId> entryRepo,
    ICurrentUser currentUser)
    : IRequestHandler<ResolveDynamicEnumOptionsQuery, Result<IReadOnlyList<DynamicEnumOptionDto>>>
{
    private static readonly JsonSerializerOptions _jsonOpts =
        new() { PropertyNameCaseInsensitive = true };

    public async Task<Result<IReadOnlyList<DynamicEnumOptionDto>>> Handle(
        ResolveDynamicEnumOptionsQuery request,
        CancellationToken cancellationToken)
    {
        if (currentUser.SiteId is not { } siteId)
            return Result.Failure<IReadOnlyList<DynamicEnumOptionDto>>(
                Error.Validation("Auth.NoSiteContext", "No site context in token."));

        var ct = await ctRepo.GetByIdAsync(new ContentTypeId(request.ContentTypeId), cancellationToken)
            ?? throw new NotFoundException(nameof(ContentType), request.ContentTypeId);

        var field = ct.Fields.FirstOrDefault(f => f.Id == request.FieldId)
            ?? throw new NotFoundException("Field", request.FieldId);

        var src = field.Validation?.DynamicSource;
        if (src is null)
            return Result.Success<IReadOnlyList<DynamicEnumOptionDto>>([]);

        // Resolve the source content type by handle.
        var allTypes = await ctRepo.ListAsync(
            new ContentTypesBySiteSpec(siteId), cancellationToken);

        var sourceCt = allTypes.FirstOrDefault(c =>
            c.Handle.Equals(src.ContentTypeHandle, StringComparison.OrdinalIgnoreCase))
            ?? throw new NotFoundException("Source ContentType", src.ContentTypeHandle);

        var spec = new EntriesBySiteSpec(
            siteId,
            statusFilter: src.StatusFilter,
            contentTypeId: sourceCt.Id.Value,
            locale: null,
            folderId: null,
            pageNumber: 1,
            pageSize: 1000);

        var entries = await entryRepo.ListAsync(spec, cancellationToken);

        var labelField = src.LabelField ?? string.Empty;
        var valueField = src.ValueField ?? string.Empty;

        var options = entries
            .Select(e => BuildOption(e, labelField, valueField))
            .Where(o => o is not null)
            .Select(o => o!)
            .ToList();

        return Result.Success<IReadOnlyList<DynamicEnumOptionDto>>(options);
    }

    private static DynamicEnumOptionDto? BuildOption(Entry entry, string labelField, string valueField)
    {
        try
        {
            using var doc = JsonDocument.Parse(entry.FieldsJson);
            var root = doc.RootElement;
            var label = TryGetString(root, labelField) ?? entry.Slug.Value;
            var value = TryGetString(root, valueField) ?? entry.Slug.Value;
            return new DynamicEnumOptionDto(label, value);
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

internal sealed class ResolveDynamicEnumOptionsBySourceQueryHandler(
    IRepository<ContentType, ContentTypeId> ctRepo,
    IRepository<Entry, EntryId> entryRepo,
    ICurrentUser currentUser)
    : IRequestHandler<ResolveDynamicEnumOptionsBySourceQuery, Result<IReadOnlyList<DynamicEnumOptionDto>>>
{
    public async Task<Result<IReadOnlyList<DynamicEnumOptionDto>>> Handle(
        ResolveDynamicEnumOptionsBySourceQuery request,
        CancellationToken cancellationToken)
    {
        if (currentUser.SiteId is not { } siteId)
            return Result.Failure<IReadOnlyList<DynamicEnumOptionDto>>(
                Error.Validation("Auth.NoSiteContext", "No site context in token."));

        var allTypes = await ctRepo.ListAsync(
            new ContentTypesBySiteSpec(siteId), cancellationToken);

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

        var options = entries
            .Select(e => BuildOption(e, request.LabelField, request.ValueField))
            .Where(o => o is not null)
            .Select(o => o!)
            .ToList();

        return Result.Success<IReadOnlyList<DynamicEnumOptionDto>>(options);
    }

    private static DynamicEnumOptionDto? BuildOption(Entry entry, string labelField, string valueField)
    {
        try
        {
            using var doc = JsonDocument.Parse(entry.FieldsJson);
            var root = doc.RootElement;
            var label = TryGetString(root, labelField) ?? entry.Slug.Value;
            var value = TryGetString(root, valueField) ?? entry.Slug.Value;
            return new DynamicEnumOptionDto(label, value);
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
