using System.Text.Json;
using MediatR;
using MicroCMS.Application.Common.Exceptions;
using MicroCMS.Application.Common.Interfaces;
using MicroCMS.Application.Features.Layouts.Commands;
using MicroCMS.Application.Features.Layouts.Dtos;
using MicroCMS.Application.Features.Layouts.Queries;
using MicroCMS.Application.Features.Layouts.Services;
using MicroCMS.Domain.Aggregates.Components;
using MicroCMS.Domain.Repositories;
using MicroCMS.Domain.Specifications.Layouts;
using MicroCMS.Shared.Ids;
using MicroCMS.Shared.Results;

namespace MicroCMS.Application.Features.Layouts.Handlers;

// ── Mapper ────────────────────────────────────────────────────────────────────

internal static class LayoutMapper
{
    private static readonly JsonSerializerOptions _json = new() { PropertyNameCaseInsensitive = true };

    internal static LayoutDto ToDto(Layout l)
    {
      var zones = DeserializeZones(l.ZonesJson);
        var placements = DeserializePlacements(l.DefaultPlacementsJson);
        return new LayoutDto(
            l.Id.Value, l.TenantId.Value, l.SiteId.Value,
       l.Name, l.Key, l.TemplateType.ToString(), l.ShellTemplate,
      l.IsDefault, zones, placements, l.CreatedAt, l.UpdatedAt);
    }

    internal static LayoutListItemDto ToListItemDto(Layout l)
    {
  var zones = DeserializeZones(l.ZonesJson);
     return new LayoutListItemDto(
            l.Id.Value, l.Name, l.Key, l.TemplateType.ToString(),
            l.IsDefault, zones.Count, l.UpdatedAt);
    }

    private static IReadOnlyList<LayoutZoneNodeDto> DeserializeZones(string json)
    {
      try
        {
            var nodes = JsonSerializer.Deserialize<List<ZoneNodeJson>>(json, _json) ?? [];
            return nodes.Select(n => new LayoutZoneNodeDto(
      n.Id, n.Type, n.Name, n.Label, n.SortOrder,
        n.Columns?.Select(c => new LayoutColumnDefDto(c.Span, c.ZoneName)).ToList()
 )).ToList().AsReadOnly();
        }
        catch { return []; }
    }

    private static IReadOnlyList<LayoutDefaultPlacementDto> DeserializePlacements(string json)
    {
        try
        {
            var items = JsonSerializer.Deserialize<List<DefaultPlacementJson>>(json, _json) ?? [];
   return items.Select(p => new LayoutDefaultPlacementDto(
  p.ComponentId, p.ComponentName, p.Zone, p.SortOrder, p.IsLocked
         )).ToList().AsReadOnly();
     }
        catch { return []; }
    }

    // Internal JSON shapes matching the stored format
    private sealed class ZoneNodeJson
    {
        public string Id { get; set; } = "";
        public string Type { get; set; } = "zone";
        public string Name { get; set; } = "";
     public string Label { get; set; } = "";
        public int SortOrder { get; set; }
    public List<ColumnJson>? Columns { get; set; }
    }
    private sealed class ColumnJson { public int Span { get; set; } public string ZoneName { get; set; } = ""; }
    private sealed class DefaultPlacementJson
    {
        public Guid ComponentId { get; set; }
        public string ComponentName { get; set; } = "";
        public string Zone { get; set; } = "";
        public int SortOrder { get; set; }
        public bool IsLocked { get; set; }
    }
}

// ── Command handlers ──────────────────────────────────────────────────────────

internal sealed class CreateLayoutCommandHandler(
    IRepository<Layout, LayoutId> repo,
    ICurrentUser currentUser,
    LayoutShellGeneratorService shellGenerator)
    : IRequestHandler<CreateLayoutCommand, Result<LayoutDto>>
{
    public async Task<Result<LayoutDto>> Handle(CreateLayoutCommand request, CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<LayoutTemplateType>(request.TemplateType, true, out var templateType))
            templateType = LayoutTemplateType.Handlebars;

        var layout = Layout.Create(
     currentUser.TenantId, new SiteId(request.SiteId),
            request.Name, request.Key, templateType);

        // Generate shell from default zones (header/content/footer)
        var shell = shellGenerator.Generate(layout.ZonesJson, request.TemplateType);
        layout.SetGeneratedShell(shell);

        await repo.AddAsync(layout, cancellationToken);
      return Result.Success(LayoutMapper.ToDto(layout));
  }
}

internal sealed class UpdateLayoutCommandHandler(
    IRepository<Layout, LayoutId> repo)
    : IRequestHandler<UpdateLayoutCommand, Result<LayoutDto>>
{
    public async Task<Result<LayoutDto>> Handle(UpdateLayoutCommand request, CancellationToken cancellationToken)
    {
        var layout = await repo.GetByIdAsync(new LayoutId(request.LayoutId), cancellationToken)
            ?? throw new NotFoundException(nameof(Layout), request.LayoutId);

        if (!Enum.TryParse<LayoutTemplateType>(request.TemplateType, true, out var templateType))
         templateType = LayoutTemplateType.Handlebars;

      layout.Update(request.Name, templateType);
        repo.Update(layout);
        return Result.Success(LayoutMapper.ToDto(layout));
    }
}

internal sealed class UpdateLayoutZonesCommandHandler(
    IRepository<Layout, LayoutId> repo,
    LayoutShellGeneratorService shellGenerator)
    : IRequestHandler<UpdateLayoutZonesCommand, Result<LayoutDto>>
{
    public async Task<Result<LayoutDto>> Handle(UpdateLayoutZonesCommand request, CancellationToken cancellationToken)
    {
        var layout = await repo.GetByIdAsync(new LayoutId(request.LayoutId), cancellationToken)
      ?? throw new NotFoundException(nameof(Layout), request.LayoutId);

      var zonesJson = JsonSerializer.Serialize(request.Zones);
        layout.UpdateZones(zonesJson);

        var shell = shellGenerator.Generate(zonesJson, layout.TemplateType.ToString());
        layout.SetGeneratedShell(shell);

        repo.Update(layout);
        return Result.Success(LayoutMapper.ToDto(layout));
    }
}

internal sealed class UpdateLayoutDefaultPlacementsCommandHandler(
    IRepository<Layout, LayoutId> repo)
    : IRequestHandler<UpdateLayoutDefaultPlacementsCommand, Result<LayoutDto>>
{
    public async Task<Result<LayoutDto>> Handle(UpdateLayoutDefaultPlacementsCommand request, CancellationToken cancellationToken)
    {
 var layout = await repo.GetByIdAsync(new LayoutId(request.LayoutId), cancellationToken)
       ?? throw new NotFoundException(nameof(Layout), request.LayoutId);

        var json = JsonSerializer.Serialize(request.Placements);
        layout.UpdateDefaultPlacements(json);
  repo.Update(layout);
        return Result.Success(LayoutMapper.ToDto(layout));
    }
}

internal sealed class SetDefaultLayoutCommandHandler(
    IRepository<Layout, LayoutId> repo)
    : IRequestHandler<SetDefaultLayoutCommand, Result<LayoutDto>>
{
    public async Task<Result<LayoutDto>> Handle(SetDefaultLayoutCommand request, CancellationToken cancellationToken)
    {
     var siteId = new SiteId(request.SiteId);
        var existing = await repo.ListAsync(new DefaultLayoutBySiteSpec(siteId), cancellationToken);
        foreach (var l in existing) { l.ClearDefault(); repo.Update(l); }

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
 public async Task<Result> Handle(DeleteLayoutCommand request, CancellationToken cancellationToken)
  {
        var layout = await repo.GetByIdAsync(new LayoutId(request.LayoutId), cancellationToken)
?? throw new NotFoundException(nameof(Layout), request.LayoutId);
        repo.Remove(layout);
 return Result.Success();
    }
}

// ── Query handlers ────────────────────────────────────────────────────────────

internal sealed class ListLayoutsQueryHandler(IRepository<Layout, LayoutId> repo)
 : IRequestHandler<ListLayoutsQuery, Result<IReadOnlyList<LayoutListItemDto>>>
{
    public async Task<Result<IReadOnlyList<LayoutListItemDto>>> Handle(
        ListLayoutsQuery request, CancellationToken cancellationToken)
    {
        var items = await repo.ListAsync(new LayoutsBySiteSpec(new SiteId(request.SiteId)), cancellationToken);
        return Result.Success<IReadOnlyList<LayoutListItemDto>>(
   items.Select(LayoutMapper.ToListItemDto).ToList().AsReadOnly());
  }
}

internal sealed class GetLayoutQueryHandler(IRepository<Layout, LayoutId> repo)
    : IRequestHandler<GetLayoutQuery, Result<LayoutDto>>
{
    public async Task<Result<LayoutDto>> Handle(GetLayoutQuery request, CancellationToken cancellationToken)
    {
        var layout = await repo.GetByIdAsync(new LayoutId(request.LayoutId), cancellationToken)
       ?? throw new NotFoundException(nameof(Layout), request.LayoutId);
    return Result.Success(LayoutMapper.ToDto(layout));
    }
}
