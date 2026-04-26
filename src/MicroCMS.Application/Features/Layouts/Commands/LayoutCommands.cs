using MicroCMS.Application.Common.Attributes;
using MicroCMS.Application.Common.Authorization;
using MicroCMS.Application.Common.Markers;
using MicroCMS.Application.Features.Layouts.Dtos;

namespace MicroCMS.Application.Features.Layouts.Commands;

[HasPolicy(ContentPolicies.ComponentManage)]
public sealed record CreateLayoutCommand(
    Guid SiteId,
    string Name,
    string Key,
    string TemplateType,
    string? ShellTemplate,
    bool IsDefault = false) : ICommand<LayoutDto>;

[HasPolicy(ContentPolicies.ComponentManage)]
public sealed record UpdateLayoutCommand(
    Guid LayoutId,
    string Name,
    string TemplateType,
    string? ShellTemplate) : ICommand<LayoutDto>;

[HasPolicy(ContentPolicies.ComponentManage)]
public sealed record SetDefaultLayoutCommand(
    Guid SiteId,
    Guid LayoutId) : ICommand<LayoutDto>;

[HasPolicy(ContentPolicies.ComponentManage)]
public sealed record DeleteLayoutCommand(Guid LayoutId) : ICommand;
