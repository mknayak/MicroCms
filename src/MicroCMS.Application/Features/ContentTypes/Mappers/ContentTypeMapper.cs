using MicroCMS.Application.Features.ContentTypes.Dtos;
using MicroCMS.Domain.Aggregates.Content;

namespace MicroCMS.Application.Features.ContentTypes.Mappers;

public static class ContentTypeMapper
{
public static ContentTypeDto ToDto(ContentType ct) => new(
        ct.Id.Value,
        ct.TenantId.Value,
        ct.SiteId.Value,
   ct.Handle,
        ct.DisplayName,
        ct.Description,
        ct.Status.ToString(),
        ct.CreatedAt,
      ct.UpdatedAt,
        ct.Fields.Select(ToFieldDto).ToList().AsReadOnly());

    public static ContentTypeListItemDto ToListItemDto(ContentType ct) => new(
        ct.Id.Value,
     ct.Handle,
      ct.DisplayName,
     ct.Status.ToString(),
        ct.Fields.Count,
      ct.UpdatedAt);

    private static FieldDefinitionDto ToFieldDto(FieldDefinition f) => new(
        f.Id,
    f.Handle,
        f.Label,
     f.FieldType.ToString(),
        f.IsRequired,
        f.IsLocalized,
        f.IsUnique,
        f.SortOrder,
        f.Description);
}
