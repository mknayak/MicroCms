using MicroCMS.Application.Common.Attributes;
using MicroCMS.Application.Common.Authorization;
using MicroCMS.Application.Common.Markers;
using MicroCMS.Application.Features.ContentTypes.Dtos;
using MicroCMS.Shared.Primitives;

namespace MicroCMS.Application.Features.ContentTypes.Commands;

[HasPolicy(ContentPolicies.ContentTypeManage)]
public sealed record CreateContentTypeCommand(
    Guid SiteId,
    string Handle,
  string DisplayName,
    string? Description = null) : ICommand<ContentTypeDto>;

[HasPolicy(ContentPolicies.ContentTypeManage)]
public sealed record AddFieldCommand(
    Guid ContentTypeId,
    string Handle,
    string Label,
    string FieldType,
    bool IsRequired = false,
    bool IsLocalized = false,
 bool IsUnique = false,
    string? Description = null) : ICommand<ContentTypeDto>;

[HasPolicy(ContentPolicies.ContentTypeManage)]
public sealed record RemoveFieldCommand(
    Guid ContentTypeId,
    Guid FieldId) : ICommand<ContentTypeDto>;

[HasPolicy(ContentPolicies.ContentTypeManage)]
public sealed record PublishContentTypeCommand(Guid ContentTypeId) : ICommand<ContentTypeDto>;

[HasPolicy(ContentPolicies.ContentTypeManage)]
public sealed record ArchiveContentTypeCommand(Guid ContentTypeId) : ICommand<ContentTypeDto>;

[HasPolicy(ContentPolicies.ContentTypeManage)]
public sealed record UpdateContentTypeCommand(
    Guid ContentTypeId,
    string DisplayName,
    string? Description = null) : ICommand<ContentTypeDto>;

[HasPolicy(ContentPolicies.ContentTypeManage)]
public sealed record DeleteContentTypeCommand(Guid ContentTypeId) : ICommand;
