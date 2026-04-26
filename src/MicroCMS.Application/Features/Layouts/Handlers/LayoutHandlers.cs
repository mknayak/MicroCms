using MediatR;
using MicroCMS.Application.Common.Exceptions;
using MicroCMS.Application.Common.Interfaces;
using MicroCMS.Application.Features.Layouts.Commands;
using MicroCMS.Application.Features.Layouts.Dtos;
using MicroCMS.Application.Features.Layouts.Queries;
using MicroCMS.Domain.Aggregates.Components;
using MicroCMS.Domain.Repositories;
using MicroCMS.Domain.Specifications.Layouts;
using MicroCMS.Shared.Ids;
using MicroCMS.Shared.Results;

namespace MicroCMS.Application.Features.Layouts.Handlers;

// ── Mapper ────────────────────────────────────────────────────────────────────

internal static class LayoutMapper
{
    internal static LayoutDto ToDto(Layout l) => new(
        l.Id.Value, l.SiteId.Value, l.Name, l.Key,
        l.TemplateType.ToString(), l.ShellTemplate,
        l.IsDefault, l.CreatedAt, l.UpdatedAt);

    internal static LayoutListItemDto ToListItemDto(Layout l) => new(
        l.Id.Value, l.Name, l.Key,
        l.TemplateType.ToString(), l.IsDefault, l.UpdatedAt);
}

// ── Command handlers ──────────────────────────────────────────────────────────

internal sealed class CreateLayoutCommandHandler(
    IRepository<Layout, LayoutId> repo,
    ICurrentUser currentUser)
    : IRequestHandler<CreateLayoutCommand, Result<LayoutDto>>
{
    public async Task<Result<LayoutDto>> Handle(
     CreateLayoutCommand request, CancellationToken cancellationToken)
{
 if (!Enum.TryParse<LayoutTemplateType>(request.TemplateType, true, out var templateType))
     templateType = LayoutTemplateType.Handlebars;

  var layout = Layout.Create(
      currentUser.TenantId,
  new SiteId(request.SiteId),
            request.Name,
            request.Key,
    templateType,
         request.ShellTemplate);

        if (request.IsDefault)
    await ClearExistingDefault(repo, new SiteId(request.SiteId), cancellationToken);

        if (request.IsDefault)
            layout.MarkAsDefault();

        await repo.AddAsync(layout, cancellationToken);
        return Result.Success(LayoutMapper.ToDto(layout));
    }

  private static async Task ClearExistingDefault(
      IRepository<Layout, LayoutId> repo, SiteId siteId, CancellationToken ct)
    {
        var existing = await repo.ListAsync(new DefaultLayoutBySiteSpec(siteId), ct);
        foreach (var l in existing) { l.ClearDefault(); repo.Update(l); }
    }
}

internal sealed class UpdateLayoutCommandHandler(
    IRepository<Layout, LayoutId> repo)
    : IRequestHandler<UpdateLayoutCommand, Result<LayoutDto>>
{
    public async Task<Result<LayoutDto>> Handle(
      UpdateLayoutCommand request, CancellationToken cancellationToken)
    {
        var layout = await repo.GetByIdAsync(new LayoutId(request.LayoutId), cancellationToken)
            ?? throw new NotFoundException(nameof(Layout), request.LayoutId);

        if (!Enum.TryParse<LayoutTemplateType>(request.TemplateType, true, out var templateType))
            templateType = LayoutTemplateType.Handlebars;

    layout.Update(request.Name, templateType, request.ShellTemplate);
        repo.Update(layout);
        return Result.Success(LayoutMapper.ToDto(layout));
    }
}

internal sealed class SetDefaultLayoutCommandHandler(
    IRepository<Layout, LayoutId> repo)
    : IRequestHandler<SetDefaultLayoutCommand, Result<LayoutDto>>
{
public async Task<Result<LayoutDto>> Handle(
        SetDefaultLayoutCommand request, CancellationToken cancellationToken)
    {
        // Clear existing default for this site
        var siteId = new SiteId(request.SiteId);
        var existing = await repo.ListAsync(new DefaultLayoutBySiteSpec(siteId), cancellationToken);
   foreach (var l in existing) { l.ClearDefault(); repo.Update(l); }

        // Set new default
        var layout = await repo.GetByIdAsync(new LayoutId(request.LayoutId), cancellationToken)
            ?? throw new NotFoundException(nameof(Layout), request.LayoutId);

     layout.MarkAsDefault();
        repo.Update(layout);
        return Result.Success(LayoutMapper.ToDto(layout));
    }
}

internal sealed class DeleteLayoutCommandHandler(
    IRepository<Layout, LayoutId> repo)
    : IRequestHandler<DeleteLayoutCommand, Result>
{
    public async Task<Result> Handle(
   DeleteLayoutCommand request, CancellationToken cancellationToken)
 {
        var layout = await repo.GetByIdAsync(new LayoutId(request.LayoutId), cancellationToken)
     ?? throw new NotFoundException(nameof(Layout), request.LayoutId);

        repo.Remove(layout);
        return Result.Success();
    }
}

// ── Query handlers ────────────────────────────────────────────────────────────

internal sealed class ListLayoutsQueryHandler(
    IRepository<Layout, LayoutId> repo)
    : IRequestHandler<ListLayoutsQuery, Result<IReadOnlyList<LayoutListItemDto>>>
{
    public async Task<Result<IReadOnlyList<LayoutListItemDto>>> Handle(
        ListLayoutsQuery request, CancellationToken cancellationToken)
    {
        var items = await repo.ListAsync(
            new LayoutsBySiteSpec(new SiteId(request.SiteId)), cancellationToken);

        return Result.Success<IReadOnlyList<LayoutListItemDto>>(
     items.Select(LayoutMapper.ToListItemDto).ToList().AsReadOnly());
    }
}

internal sealed class GetLayoutQueryHandler(
    IRepository<Layout, LayoutId> repo)
    : IRequestHandler<GetLayoutQuery, Result<LayoutDto>>
{
public async Task<Result<LayoutDto>> Handle(
        GetLayoutQuery request, CancellationToken cancellationToken)
    {
        var layout = await repo.GetByIdAsync(new LayoutId(request.LayoutId), cancellationToken)
       ?? throw new NotFoundException(nameof(Layout), request.LayoutId);

        return Result.Success(LayoutMapper.ToDto(layout));
    }
}
