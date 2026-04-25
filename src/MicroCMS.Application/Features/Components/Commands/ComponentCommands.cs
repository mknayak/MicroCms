using MicroCMS.Application.Common.Attributes;
using MicroCMS.Application.Common.Authorization;
using MicroCMS.Application.Common.Markers;
using MicroCMS.Application.Features.Components.Dtos;
using MicroCMS.Shared.Primitives;

namespace MicroCMS.Application.Features.Components.Commands;

public sealed record ComponentFieldInput(
    string Handle,
    string Label,
    string FieldType,
    bool IsRequired,
    bool IsLocalized,
    bool IsIndexed,
    int SortOrder,
    string? Description);

[HasPolicy(ContentPolicies.ComponentManage)]
public sealed record CreateComponentCommand(
    Guid SiteId,
    string Name,
    string Key,
    string? Description,
    string Category,
    IReadOnlyList<string> Zones,
    IReadOnlyList<ComponentFieldInput>? Fields = null) : ICommand<ComponentDto>;

[HasPolicy(ContentPolicies.ComponentManage)]
public sealed record UpdateComponentCommand(
 Guid ComponentId,
    string Name,
    string? Description,
    string Category,
    IReadOnlyList<string> Zones,
    IReadOnlyList<ComponentFieldInput> Fields) : ICommand<ComponentDto>;

[HasPolicy(ContentPolicies.ComponentManage)]
public sealed record DeleteComponentCommand(Guid ComponentId) : ICommand;

/// <summary>Creates or replaces the rendering template for a component definition.</summary>
[HasPolicy(ContentPolicies.ComponentManage)]
public sealed record UpdateComponentTemplateCommand(
    Guid ComponentId,
    string TemplateType,
    string? TemplateContent) : ICommand<ComponentDto>;

[HasPolicy(ContentPolicies.ComponentManage)]
public sealed record CreateComponentItemCommand(
    Guid ComponentId,
    string Title,
    string FieldsJson) : ICommand<ComponentItemDto>;

[HasPolicy(ContentPolicies.ComponentManage)]
public sealed record UpdateComponentItemCommand(
    Guid ComponentId,
    Guid ItemId,
    string Title,
    string FieldsJson) : ICommand<ComponentItemDto>;

[HasPolicy(ContentPolicies.ComponentManage)]
public sealed record PublishComponentItemCommand(Guid ComponentId, Guid ItemId) : ICommand;

[HasPolicy(ContentPolicies.ComponentManage)]
public sealed record ArchiveComponentItemCommand(Guid ComponentId, Guid ItemId) : ICommand;

[HasPolicy(ContentPolicies.ComponentManage)]
public sealed record DeleteComponentItemCommand(Guid ComponentId, Guid ItemId) : ICommand;
