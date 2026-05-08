using System.Text.Json.Serialization;

namespace MicroCMS.Application.Features.Layouts.Dtos;

public sealed record LayoutColumnDefDto(int Span, string ZoneName);

public sealed record LayoutZoneNodeDto(
    string Id,
    string Type,   // "zone" | "grid-row"
    string Name,
    string Label,
    int SortOrder,
    IReadOnlyList<LayoutColumnDefDto>? Columns = null);

public sealed record LayoutDefaultPlacementDto(
    Guid ComponentId,
    string ComponentName,
    string Zone,
    int SortOrder,
  bool IsLocked);

// ── Layout Config DTOs ────────────────────────────────────────────────────────

/// <summary>
/// A single asset entry in the layout configuration.
/// Token values (e.g. <c>{{page:slug}}</c>) inside <see cref="Content"/> or
/// <see cref="Href"/>/<see cref="Src"/> are stored verbatim and resolved at render time.
/// </summary>
public sealed record LayoutAssetDto(
    /// <summary>Stable client-generated identifier for the asset entry.</summary>
    string Id,
    /// <summary>Sort key. Gaps of 10 recommended to allow insert without full reorder.</summary>
    int Order,
    /// <summary>Asset kind: <c>inline-css</c> | <c>inline-js</c> | <c>raw-html</c></summary>
    string Type,
    /// <summary>Injection position: <c>head</c> | <c>body-start</c> | <c>body-end</c></summary>
    string Position,
    string? Content,
    /// <summary>When <c>true</c>, the generated script tag will receive a CSP nonce attribute (resolved in Sprint 19).</summary>
    bool Nonce,
    /// <summary>Extra HTML attributes, e.g. <c>integrity</c>, <c>crossorigin</c>.</summary>
    IReadOnlyDictionary<string, string> Attributes);

/// <summary>
/// A single <c>&lt;body&gt;</c> attribute entry.
/// <see cref="Value"/> may contain token placeholders such as <c>{{page:slug}}</c>.
/// </summary>
public sealed record LayoutBodyAttributeDto(
    string Attribute,
    string Value);

/// <summary>
/// Root configuration object stored in <c>Layout.LayoutConfigJson</c>.
/// </summary>
public sealed record LayoutConfigDto(
    IReadOnlyList<LayoutAssetDto> Assets,
    IReadOnlyList<LayoutBodyAttributeDto> BodyAttributes);

// ── Layout DTOs ───────────────────────────────────────────────────────────────

public sealed record LayoutDto(
    Guid Id,
    Guid TenantId,
    Guid SiteId,
    string Name,
    string Key,
    string TemplateType,
    string? ShellTemplate,
    bool IsShellCustomized,
    bool IsDefault,
    IReadOnlyList<LayoutZoneNodeDto> Zones,
    IReadOnlyList<LayoutDefaultPlacementDto> DefaultPlacements,
    LayoutConfigDto Config,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record LayoutListItemDto(
    Guid Id,
    string Name,
    string Key,
    string TemplateType,
    bool IsDefault,
    int ZoneCount,
    DateTimeOffset UpdatedAt);
