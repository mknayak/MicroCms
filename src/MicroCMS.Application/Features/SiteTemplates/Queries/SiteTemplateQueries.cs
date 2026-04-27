using MicroCMS.Application.Common.Attributes;
using MicroCMS.Application.Common.Authorization;
using MicroCMS.Application.Common.Markers;
using MicroCMS.Application.Features.SiteTemplates.Dtos;

namespace MicroCMS.Application.Features.SiteTemplates.Queries;

[HasPolicy(ContentPolicies.PageTemplateRead)]
public sealed record GetSiteTemplateQuery(Guid TemplateId) : IQuery<SiteTemplateDto>;

[HasPolicy(ContentPolicies.PageTemplateRead)]
public sealed record ListSiteTemplatesQuery(Guid SiteId) : IQuery<IReadOnlyList<SiteTemplateListItemDto>>;
