using MicroCMS.Application.Common.Attributes;
using MicroCMS.Application.Common.Authorization;
using MicroCMS.Application.Common.Markers;
using MicroCMS.Application.Features.Layouts.Dtos;

namespace MicroCMS.Application.Features.Layouts.Commands;

[HasPolicy(ContentPolicies.LayoutManage)]
public sealed record CreateLayoutCommand(
    Guid SiteId,
    string Name,
    string Key,
    string TemplateType) : ICommand<LayoutDto>;

[HasPolicy(ContentPolicies.LayoutManage)]
public sealed record UpdateLayoutCommand(
    Guid LayoutId,
    string Name,
    string TemplateType) : ICommand<LayoutDto>;

[HasPolicy(ContentPolicies.LayoutManage)]
public sealed record UpdateLayoutZonesCommand(
    Guid LayoutId,
    IReadOnlyList<LayoutZoneNodeDto> Zones) : ICommand<LayoutDto>;

[HasPolicy(ContentPolicies.LayoutManage)]
public sealed record UpdateLayoutDefaultPlacementsCommand(
    Guid LayoutId,
    IReadOnlyList<LayoutDefaultPlacementDto> Placements) : ICommand<LayoutDto>;

[HasPolicy(ContentPolicies.LayoutManage)]
public sealed record SetDefaultLayoutCommand(
    Guid SiteId,
    Guid LayoutId) : ICommand<LayoutDto>;

[HasPolicy(ContentPolicies.LayoutManage)]
public sealed record DeleteLayoutCommand(Guid LayoutId) : ICommand;
