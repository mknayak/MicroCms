using MicroCMS.Application.Features.ContentTypes.Dtos;
using MicroCMS.Domain.Aggregates.Content;

namespace MicroCMS.Application.Features.ContentTypes.Mappers;

public static class ContentTypeMapper
{
    /// <summary>
    /// Maps a content type to its DTO.
    /// When <paramref name="parent"/> is provided, parent fields are merged in first
    /// (tagged <c>IsInherited = true</c>). Child fields with the same handle override the parent.
    /// </summary>
    public static ContentTypeDto ToDto(ContentType ct, ContentType? parent = null)
    {
        IReadOnlyList<FieldDefinitionDto> fields;

        if (parent is not null)
        {
            // Build merged field list: parent first, then child overrides
            var parentFields = parent.Fields
                .OrderBy(f => f.SortOrder)
                .Select(f => ToFieldDto(f, isInherited: true))
                .ToList();

            var childHandles = ct.Fields.Select(f => f.Handle).ToHashSet(StringComparer.OrdinalIgnoreCase);

            // Remove parent entries that are overridden by a child field
            var mergedParent = parentFields
                .Where(f => !childHandles.Contains(f.Handle))
                .ToList();

            var childFields = ct.Fields
                .OrderBy(f => f.SortOrder)
                .Select(f => ToFieldDto(f, isInherited: false))
                .ToList();

            fields = mergedParent.Concat(childFields).ToList().AsReadOnly();
        }
        else
        {
            fields = ct.Fields.OrderBy(f => f.SortOrder).Select(f => ToFieldDto(f)).ToList().AsReadOnly();
        }

        return new ContentTypeDto(
            ct.Id.Value,
            ct.TenantId.Value,
            ct.SiteId.Value,
            ct.Handle,
            ct.DisplayName,
            ct.Description,
            ct.LocalizationMode.ToString(),
            ct.Status.ToString(),
            ct.Kind.ToString(),
            ct.SiteTemplateId?.Value,
            ct.CreatedAt,
            ct.UpdatedAt,
            fields,
            ParentContentTypeId: ct.ParentContentTypeId?.Value,
            ParentHandle: parent?.Handle);
    }

    public static ContentTypeListItemDto ToListItemDto(ContentType ct,
        int entryCount = 0, int localeCount = 0) => new(
        ct.Id.Value,
        ct.Handle,
        ct.DisplayName,
        ct.Status.ToString(),
        ct.LocalizationMode.ToString(),
        ct.Kind.ToString(),
        ct.Fields.Count,
        entryCount,
        localeCount,
        ct.UpdatedAt);

    internal static FieldDefinitionDto ToFieldDto(FieldDefinition f, bool isInherited = false)
    {
        var validation = f.Validation;
        return new FieldDefinitionDto(
            f.Id,
            f.Handle,
            f.Label,
            f.FieldType.ToString(),
            f.IsRequired,
            f.IsLocalized,
            f.IsUnique,
            f.IsIndexed,
            f.IsList,
            f.SortOrder,
            f.Description,
            f.GroupName,
            IsInherited: isInherited,
            Options: validation?.Options,
            DynamicSource: validation?.DynamicSource,
            MultiListSource: validation?.MultiListSource);
    }
}
