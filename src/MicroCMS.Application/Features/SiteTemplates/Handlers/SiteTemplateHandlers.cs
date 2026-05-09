using System.Text.Json;
using MediatR;
using MicroCMS.Application.Common.Exceptions;
using MicroCMS.Application.Common.Interfaces;
using MicroCMS.Application.Features.SiteTemplates.Commands;
using MicroCMS.Application.Features.SiteTemplates.Dtos;
using MicroCMS.Application.Features.SiteTemplates.Queries;
using MicroCMS.Domain.Aggregates.Components;
using MicroCMS.Domain.Aggregates.Content;
using MicroCMS.Domain.Aggregates.Pages;
using MicroCMS.Domain.Repositories;
using MicroCMS.Domain.Specifications.Content;
using MicroCMS.Domain.Specifications.SiteTemplates;
using MicroCMS.Shared.Ids;
using MicroCMS.Shared.Results;

namespace MicroCMS.Application.Features.SiteTemplates.Handlers;

// ── Mapper ────────────────────────────────────────────────────────────────────

internal static class SiteTemplateMapper
{
    internal static SiteTemplateDto ToDto(SiteTemplate t, string? layoutName = null) =>
        new(t.Id.Value, t.TenantId.Value, t.SiteId.Value,
            t.LayoutId.Value, layoutName,
    t.Name, t.Description,
        t.PlacementsJson,
  t.CreatedAt, t.UpdatedAt);

    internal static SiteTemplateListItemDto ToListItemDto(SiteTemplate t, string layoutName, int pageCount) =>
        new(t.Id.Value, t.Name, t.Description,
            t.LayoutId.Value, layoutName, pageCount, t.UpdatedAt);
}

// ── Command Handlers ──────────────────────────────────────────────────────────

internal sealed class CreateSiteTemplateCommandHandler(
    IRepository<SiteTemplate, SiteTemplateId> repo,
    IRepository<Layout, LayoutId> layoutRepo,
    ICurrentUser currentUser)
    : IRequestHandler<CreateSiteTemplateCommand, Result<SiteTemplateDto>>
{
    public async Task<Result<SiteTemplateDto>> Handle(
        CreateSiteTemplateCommand request, CancellationToken cancellationToken)
    {
      var layoutId = new LayoutId(request.LayoutId);
        var layout = await layoutRepo.GetByIdAsync(layoutId, cancellationToken)
            ?? throw new NotFoundException(nameof(Layout), request.LayoutId);

        var template = SiteTemplate.Create(
currentUser.TenantId,
   currentUser.SiteId ?? new SiteId(Guid.Empty),
   layoutId,
     request.Name,
   request.Description);

        await repo.AddAsync(template, cancellationToken);
  return Result.Success(SiteTemplateMapper.ToDto(template, layout.Name));
    }
}

internal sealed class UpdateSiteTemplateCommandHandler(
    IRepository<SiteTemplate, SiteTemplateId> repo,
    IRepository<Layout, LayoutId> layoutRepo)
    : IRequestHandler<UpdateSiteTemplateCommand, Result<SiteTemplateDto>>
{
    public async Task<Result<SiteTemplateDto>> Handle(
 UpdateSiteTemplateCommand request, CancellationToken cancellationToken)
    {
    var template = await repo.GetByIdAsync(new SiteTemplateId(request.TemplateId), cancellationToken)
        ?? throw new NotFoundException(nameof(SiteTemplate), request.TemplateId);

    var layoutId = new LayoutId(request.LayoutId);
        var layout = await layoutRepo.GetByIdAsync(layoutId, cancellationToken)
            ?? throw new NotFoundException(nameof(Layout), request.LayoutId);

   template.Update(request.Name, request.Description, layoutId);
        repo.Update(template);
        return Result.Success(SiteTemplateMapper.ToDto(template, layout.Name));
    }
}

internal sealed class SaveSiteTemplatePlacementsCommandHandler(
    IRepository<SiteTemplate, SiteTemplateId> repo)
    : IRequestHandler<SaveSiteTemplatePlacementsCommand, Result<SiteTemplateDto>>
{
    public async Task<Result<SiteTemplateDto>> Handle(
        SaveSiteTemplatePlacementsCommand request, CancellationToken cancellationToken)
{
        var template = await repo.GetByIdAsync(new SiteTemplateId(request.TemplateId), cancellationToken)
          ?? throw new NotFoundException(nameof(SiteTemplate), request.TemplateId);

   template.SavePlacements(request.PlacementsJson);
        repo.Update(template);
        return Result.Success(SiteTemplateMapper.ToDto(template));
    }
}

internal sealed class DeleteSiteTemplateCommandHandler(
    IRepository<SiteTemplate, SiteTemplateId> repo)
    : IRequestHandler<DeleteSiteTemplateCommand, Result>
{
    public async Task<Result> Handle(
    DeleteSiteTemplateCommand request, CancellationToken cancellationToken)
    {
     var template = await repo.GetByIdAsync(new SiteTemplateId(request.TemplateId), cancellationToken)
        ?? throw new NotFoundException(nameof(SiteTemplate), request.TemplateId);

        repo.Remove(template);
      return Result.Success();
    }
}

// ── Query Handlers ────────────────────────────────────────────────────────────

internal sealed class GetSiteTemplateQueryHandler(
    IRepository<SiteTemplate, SiteTemplateId> repo,
    IRepository<Layout, LayoutId> layoutRepo)
    : IRequestHandler<GetSiteTemplateQuery, Result<SiteTemplateDto>>
{
    public async Task<Result<SiteTemplateDto>> Handle(
        GetSiteTemplateQuery request, CancellationToken cancellationToken)
    {
   var template = await repo.GetByIdAsync(new SiteTemplateId(request.TemplateId), cancellationToken)
  ?? throw new NotFoundException(nameof(SiteTemplate), request.TemplateId);

        var layout = await layoutRepo.GetByIdAsync(template.LayoutId, cancellationToken);
        return Result.Success(SiteTemplateMapper.ToDto(template, layout?.Name));
 }
}

internal sealed class ListSiteTemplatesQueryHandler(
    IRepository<SiteTemplate, SiteTemplateId> repo,
    IRepository<Layout, LayoutId> layoutRepo,
    ICurrentUser currentUser)
 : IRequestHandler<ListSiteTemplatesQuery, Result<IReadOnlyList<SiteTemplateListItemDto>>>
{
    public async Task<Result<IReadOnlyList<SiteTemplateListItemDto>>> Handle(
   ListSiteTemplatesQuery request, CancellationToken cancellationToken)
    {
        if (currentUser.SiteId is not { } siteId)
            return Result.Failure<IReadOnlyList<SiteTemplateListItemDto>>(Error.Validation("Auth.NoSiteContext", "No site context in token. Call POST /auth/switch-site first."));

        var templates = await repo.ListAsync(
            new SiteTemplatesBySiteSpec(siteId), cancellationToken);

        // Resolve layout names in one pass — avoid N+1 with a local dict
        var layoutIds = templates.Select(t => t.LayoutId).Distinct().ToList();
        var layouts = new Dictionary<LayoutId, string>();
        foreach (var lid in layoutIds)
      {
            var l = await layoutRepo.GetByIdAsync(lid, cancellationToken);
            if (l is not null) layouts[lid] = l.Name;
        }

        var dtos = templates
            .Select(t => SiteTemplateMapper.ToListItemDto(
                t,
            layouts.GetValueOrDefault(t.LayoutId, "—"),
   pageCount: 0))   // TODO: join pages table when Page.SiteTemplateId index is available
  .ToList()
            .AsReadOnly();

  return Result.Success<IReadOnlyList<SiteTemplateListItemDto>>(dtos);
  }
}

/// <summary>
/// Resolves the effective SiteTemplate for a page by walking:
///   1. Page.SiteTemplateId  (explicit page-level override)
///   2. ContentType.SiteTemplateId  (default for the page's backing content type)
///   3. None  (renderer falls back to default / first Layout for the site)
/// </summary>
internal sealed class GetEffectiveTemplateQueryHandler(
    IRepository<Page, PageId> pageRepo,
    IRepository<Entry, EntryId> entryRepo,
    IRepository<ContentType, ContentTypeId> contentTypeRepo,
    IRepository<SiteTemplate, SiteTemplateId> templateRepo,
    IRepository<Layout, LayoutId> layoutRepo)
    : IRequestHandler<GetEffectiveTemplateQuery, Result<EffectiveTemplateDto>>
{
    public async Task<Result<EffectiveTemplateDto>> Handle(
        GetEffectiveTemplateQuery request, CancellationToken cancellationToken)
    {
        var page = await pageRepo.GetByIdAsync(new PageId(request.PageId), cancellationToken)
            ?? throw new NotFoundException(nameof(Page), request.PageId);

        // ── 1. Page-level override ────────────────────────────────────────
        var pageResult = await TryPageOverrideAsync(page, templateRepo, layoutRepo, cancellationToken);
        if (pageResult is not null) return Result.Success(pageResult);

        // ── 2. ContentType default ────────────────────────────────────────
        var contentTypeResult = await TryContentTypeDefaultAsync(
            page, entryRepo, contentTypeRepo, templateRepo, layoutRepo, cancellationToken);
        if (contentTypeResult is not null) return Result.Success(contentTypeResult);

        // ── 3. No SiteTemplate — caller falls back to default Layout ──────
        return Result.Success(new EffectiveTemplateDto(EffectiveTemplateSource.None, null));
    }

    private static async Task<EffectiveTemplateDto?> TryPageOverrideAsync(
        Page page,
        IRepository<SiteTemplate, SiteTemplateId> templateRepo,
        IRepository<Layout, LayoutId> layoutRepo,
        CancellationToken ct)
    {
        if (!page.SiteTemplateId.HasValue) return null;
        var t = await templateRepo.GetByIdAsync(page.SiteTemplateId.Value, ct);
        if (t is null) return null;
        var layout = await layoutRepo.GetByIdAsync(t.LayoutId, ct);
        return new EffectiveTemplateDto(EffectiveTemplateSource.PageOverride, SiteTemplateMapper.ToDto(t, layout?.Name));
    }

    private static async Task<EffectiveTemplateDto?> TryContentTypeDefaultAsync(
        Page page,
        IRepository<Entry, EntryId> entryRepo,
        IRepository<ContentType, ContentTypeId> contentTypeRepo,
        IRepository<SiteTemplate, SiteTemplateId> templateRepo,
        IRepository<Layout, LayoutId> layoutRepo,
        CancellationToken ct)
    {
        ContentTypeId? contentTypeId = page.CollectionContentTypeId;
        if (contentTypeId is null && page.LinkedEntryId is not null)
        {
            var entry = await entryRepo.GetByIdAsync(page.LinkedEntryId.Value, ct);
            contentTypeId = entry?.ContentTypeId;
        }
        if (contentTypeId is null) return null;

        var ct2 = await contentTypeRepo.GetByIdAsync(contentTypeId.Value, ct);
        if (ct2?.SiteTemplateId is not { } ctTemplateId) return null;

        var t = await templateRepo.GetByIdAsync(ctTemplateId, ct);
        if (t is null) return null;
        var layout = await layoutRepo.GetByIdAsync(t.LayoutId, ct);
        return new EffectiveTemplateDto(EffectiveTemplateSource.ContentTypeDefault, SiteTemplateMapper.ToDto(t, layout?.Name));
    }
}

