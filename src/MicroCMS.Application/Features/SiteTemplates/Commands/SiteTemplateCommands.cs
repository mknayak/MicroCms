using MicroCMS.Application.Common.Attributes;
using MicroCMS.Application.Common.Authorization;
using MicroCMS.Application.Common.Markers;
using MicroCMS.Application.Features.SiteTemplates.Dtos;

namespace MicroCMS.Application.Features.SiteTemplates.Commands;

[HasPolicy(ContentPolicies.PageTemplateManage)]
public sealed record CreateSiteTemplateCommand(
    Guid SiteId,
    Guid LayoutId,
    string Name,
    string? Description) : ICommand<SiteTemplateDto>;

[HasPolicy(ContentPolicies.PageTemplateManage)]
public sealed record UpdateSiteTemplateCommand(
    Guid TemplateId,
    Guid LayoutId,
    string Name,
    string? Description) : ICommand<SiteTemplateDto>;

[HasPolicy(ContentPolicies.PageTemplateManage)]
public sealed record SaveSiteTemplatePlacementsCommand(
    Guid TemplateId,
    string PlacementsJson) : ICommand<SiteTemplateDto>;

[HasPolicy(ContentPolicies.PageTemplateManage)]
public sealed record DeleteSiteTemplateCommand(Guid TemplateId) : ICommand;
