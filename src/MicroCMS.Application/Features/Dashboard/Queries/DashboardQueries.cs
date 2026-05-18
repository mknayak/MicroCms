using MicroCMS.Application.Common.Attributes;
using MicroCMS.Application.Common.Authorization;
using MicroCMS.Application.Common.Markers;
using MicroCMS.Application.Features.Dashboard.Dtos;
using MicroCMS.Shared.Primitives;

namespace MicroCMS.Application.Features.Dashboard.Queries;

/// <summary>Returns aggregate statistics for the current site/tenant dashboard.</summary>
[HasPolicy(ContentPolicies.EntryRead)]
public sealed record GetDashboardStatsQuery : IQuery<DashboardStatsDto>;

/// <summary>Returns a paged list of recent activity items for the current site.</summary>
[HasPolicy(ContentPolicies.EntryRead)]
public sealed record GetDashboardActivityQuery(int PageSize = 10, int PageNumber = 1)
    : IQuery<PagedList<DashboardActivityItemDto>>;
