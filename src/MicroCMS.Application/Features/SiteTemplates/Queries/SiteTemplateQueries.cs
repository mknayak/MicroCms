using MicroCMS.Application.Common.Attributes;
using MicroCMS.Application.Common.Authorization;
using MicroCMS.Application.Common.Markers;
using MicroCMS.Application.Features.SiteTemplates.Dtos;

namespace MicroCMS.Application.Features.SiteTemplates.Queries;

[HasPolicy(ContentPolicies.PageTemplateRead)]
public sealed record GetSiteTemplateQuery(Guid TemplateId) : IQuery<SiteTemplateDto>;

[HasPolicy(ContentPolicies.PageTemplateRead)]
public sealed record ListSiteTemplatesQuery() : IQuery<IReadOnlyList<SiteTemplateListItemDto>>;

/// <summary>
/// Resolves the effective SiteTemplate for a page by walking the hierarchy:
///   1. Page.SiteTemplateId (explicit page override)
///   2. ContentType.SiteTemplateId (default for the page's content type)
///   3. None (caller falls back to default Layout)
/// </summary>
[HasPolicy(ContentPolicies.PageTemplateRead)]
public sealed record GetEffectiveTemplateQuery(Guid PageId) : IQuery<EffectiveTemplateDto>;
