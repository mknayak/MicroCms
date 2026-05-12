using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MicroCMS.Application.Features.Layouts.Services;

/// <summary>
/// Generates a Handlebars or HTML shell template from a structured zone tree and layout config.
/// Called by layout command handlers whenever zones or config change.
/// </summary>
public sealed class LayoutShellGeneratorService
{
    private static readonly JsonSerializerOptions _json = new() { PropertyNameCaseInsensitive = true };

    /// <summary>
    /// Generates the full HTML shell from zones and layout configuration.
    /// Token values inside asset content or body attributes (e.g. <c>{{page:slug}}</c>) are
    /// written verbatim — they are resolved at render time, not here.
    /// </summary>
    public string Generate(string zonesJson, string layoutConfigJson, string templateType)
    {
        var zones = JsonSerializer.Deserialize<List<ZoneNodeDto>>(zonesJson, _json) ?? [];
        var config = DeserializeConfig(layoutConfigJson);
        var isHandlebars = !templateType.Equals("Html", StringComparison.OrdinalIgnoreCase);
        return isHandlebars ? BuildHandlebars(zones, config) : BuildHtml(zones, config);
    }

    // ── Handlebars builder ────────────────────────────────────────────────

    private static string BuildHandlebars(List<ZoneNodeDto> zones, LayoutConfigDto config)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html lang=\"{{page:language}}\">");
        sb.AppendLine("<head>");
        AppendHeadAssets(sb, config, "  ");
        sb.AppendLine("</head>");
        AppendOpenBodyTag(sb, config);
        AppendBodyStartAssets(sb, config, "  ");

        foreach (var zone in zones.OrderBy(z => z.SortOrder))
            AppendNodeHandlebars(sb, zone, "  ");

        AppendBodyEndAssets(sb, config, "  ");
        sb.AppendLine("</body>");
        sb.AppendLine("</html>");
        return sb.ToString();
    }

    private static void AppendNodeHandlebars(StringBuilder sb, ZoneNodeDto node, string indent)
    {
        switch (node.Type)
        {
            case "html-element":
            {
                var tag = string.IsNullOrWhiteSpace(node.Tag) ? "div" : node.Tag;
                var attrs = BuildHtmlAttrs(node);
                sb.AppendLine($"{indent}<{tag}{attrs}>");
                foreach (var child in (node.Children ?? []).OrderBy(c => c.SortOrder))
                    AppendNodeHandlebars(sb, child, indent + "  ");
                sb.AppendLine($"{indent}</{tag}>");
                break;
            }
            case "drop-zone":
            case "zone":
            {
                var token = node.Name.Replace("-", "_");
                sb.AppendLine($"{indent}<div data-zone=\"{node.Name}\">");
                sb.AppendLine($"{indent}  {{{{{{{token}}}}}}}");
                sb.AppendLine($"{indent}</div>");
                break;
            }
            case "grid-row" when node.Columns?.Count > 0:
            {
                sb.AppendLine($"{indent}<div class=\"grid-row\" data-zone-row=\"{node.Name}\">");
                foreach (var col in node.Columns)
                {
                    var token = col.ZoneName.Replace("-", "_");
                    sb.AppendLine($"{indent}  <div class=\"col-{col.Span}\" data-zone=\"{col.ZoneName}\">");
                    sb.AppendLine($"{indent}    {{{{{{{token}}}}}}}");
                    sb.AppendLine($"{indent}  </div>");
                }
                sb.AppendLine($"{indent}</div>");
                break;
            }
        }
    }

    // ── HTML builder ──────────────────────────────────────────────────────

    private static string BuildHtml(List<ZoneNodeDto> zones, LayoutConfigDto config)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html lang=\"{{page:language}}\">");
        sb.AppendLine("<head>");
        AppendHeadAssets(sb, config, "  ");
        sb.AppendLine("</head>");
        AppendOpenBodyTag(sb, config);
        AppendBodyStartAssets(sb, config, "  ");

        foreach (var zone in zones.OrderBy(z => z.SortOrder))
            AppendNodeHtml(sb, zone, "  ");

        AppendBodyEndAssets(sb, config, "  ");
        sb.AppendLine("</body>");
        sb.AppendLine("</html>");
        return sb.ToString();
    }

    private static void AppendNodeHtml(StringBuilder sb, ZoneNodeDto node, string indent)
    {
        switch (node.Type)
        {
            case "html-element":
            {
                var tag = string.IsNullOrWhiteSpace(node.Tag) ? "div" : node.Tag;
                var attrs = BuildHtmlAttrs(node);
                sb.AppendLine($"{indent}<{tag}{attrs}>");
                foreach (var child in (node.Children ?? []).OrderBy(c => c.SortOrder))
                    AppendNodeHtml(sb, child, indent + "  ");
                sb.AppendLine($"{indent}</{tag}>");
                break;
            }
            case "drop-zone":
            case "zone":
            {
                sb.AppendLine($"{indent}<div data-zone=\"{node.Name}\">");
                sb.AppendLine($"{indent}  {{{{zone:{node.Name}}}}}");
                sb.AppendLine($"{indent}</div>");
                break;
            }
            case "grid-row" when node.Columns?.Count > 0:
            {
                sb.AppendLine($"{indent}<div class=\"grid-row\" data-zone-row=\"{node.Name}\">");
                foreach (var col in node.Columns)
                {
                    sb.AppendLine($"{indent}  <div class=\"col-{col.Span}\" data-zone=\"{col.ZoneName}\">");
                    sb.AppendLine($"{indent}    {{{{zone:{col.ZoneName}}}}}");
                    sb.AppendLine($"{indent}  </div>");
                }
                sb.AppendLine($"{indent}</div>");
                break;
            }
        }
    }

    private static string BuildHtmlAttrs(ZoneNodeDto node)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(node.CssClass))
            parts.Add($"class=\"{node.CssClass}\"");
        if (node.HtmlAttributes is { Count: > 0 })
            parts.AddRange(node.HtmlAttributes.Select(kv => $"{kv.Key}=\"{kv.Value}\""));
        return parts.Count > 0 ? " " + string.Join(" ", parts) : string.Empty;
    }

    // ── Asset injection helpers ───────────────────────────────────────────

    private static void AppendHeadAssets(StringBuilder sb, LayoutConfigDto config, string indent)
    {
        foreach (var asset in config.Assets
            .Where(a => a.Position == "head")
            .OrderBy(a => a.Order))
        {
            AppendAsset(sb, asset, indent);
        }
    }

    private static void AppendBodyStartAssets(StringBuilder sb, LayoutConfigDto config, string indent)
    {
        foreach (var asset in config.Assets
            .Where(a => a.Position == "body-start")
            .OrderBy(a => a.Order))
        {
            AppendAsset(sb, asset, indent);
        }
    }

    private static void AppendBodyEndAssets(StringBuilder sb, LayoutConfigDto config, string indent)
    {
        foreach (var asset in config.Assets
            .Where(a => a.Position == "body-end")
            .OrderBy(a => a.Order))
        {
            AppendAsset(sb, asset, indent);
        }
    }

    private static void AppendAsset(StringBuilder sb, AssetDto asset, string indent)
    {
        switch (asset.Type)
        {
            case "inline-css":  AppendInlineCss(sb, asset, indent); break;
            case "inline-js":   AppendInlineJs(sb, asset, indent); break;
            case "raw-html":    sb.AppendLine(asset.Content ?? string.Empty); break;
        }
    }

    private static void AppendInlineCss(StringBuilder sb, AssetDto asset, string indent)
    {
        sb.AppendLine($"{indent}<style>");
        sb.AppendLine(asset.Content ?? string.Empty);
        sb.AppendLine($"{indent}</style>");
    }

    private static void AppendInlineJs(StringBuilder sb, AssetDto asset, string indent)
    {
        var nonceAttr = asset.Nonce ? " nonce=\"{{nonce}}\"" : string.Empty;
        sb.AppendLine($"{indent}<script{nonceAttr}>");
        sb.AppendLine(asset.Content ?? string.Empty);
        sb.AppendLine($"{indent}</script>");
    }

    private static void AppendOpenBodyTag(StringBuilder sb, LayoutConfigDto config)
    {
        if (config.BodyAttributes.Count == 0)
        {
            sb.AppendLine("<body>");
            return;
        }

        var attrs = string.Join(" ", config.BodyAttributes
            .Select(a => $"{a.Attribute}=\"{a.Value}\""));
        sb.AppendLine($"<body {attrs}>");
    }

    // ── Config deserialization ────────────────────────────────────────────

    private static LayoutConfigDto DeserializeConfig(string json)
    {
        try
        {
            var raw = JsonSerializer.Deserialize<LayoutConfigRaw>(json, _json);
            if (raw is null) return LayoutConfigDto.Empty;

            var assets = (raw.Assets ?? []).Select(a => new AssetDto(
                a.Id ?? string.Empty, a.Order, a.Type ?? string.Empty, a.Position ?? "head",
                a.Content, a.Nonce,
                (IReadOnlyDictionary<string, string>)(a.Attributes ?? new Dictionary<string, string>())
            )).ToList();

            var bodyAttrs = (raw.BodyAttributes ?? [])
                .Select(b => new BodyAttributeDto(b.Attribute ?? string.Empty, b.Value ?? string.Empty))
                .ToList();

            return new LayoutConfigDto(assets, bodyAttrs);
        }
        catch { return LayoutConfigDto.Empty; }
    }

    // ── Internal DTOs (generator-local, not exposed to Application layer) ─

    private sealed record LayoutConfigDto(
        IReadOnlyList<AssetDto> Assets,
        IReadOnlyList<BodyAttributeDto> BodyAttributes)
    {
        public static readonly LayoutConfigDto Empty =
            new(Array.AsReadOnly(Array.Empty<AssetDto>()),
                Array.AsReadOnly(Array.Empty<BodyAttributeDto>()));
    }

    private sealed record AssetDto(
        string Id, int Order, string Type, string Position,
        string? Content, bool Nonce,
        IReadOnlyDictionary<string, string> Attributes);

    private sealed record BodyAttributeDto(string Attribute, string Value);

    // ── JSON deserialization shapes ───────────────────────────────────────

    private sealed class LayoutConfigRaw
    {
        public List<AssetRaw>? Assets { get; set; }
        public List<BodyAttributeRaw>? BodyAttributes { get; set; }
    }

    private sealed class AssetRaw
    {
        public string? Id { get; set; }
        public int Order { get; set; }
        public string? Type { get; set; }
        public string? Position { get; set; }
        public string? Content { get; set; }
        public bool Nonce { get; set; }
        public Dictionary<string, string>? Attributes { get; set; }
    }

    private sealed class BodyAttributeRaw
    {
        public string? Attribute { get; set; }
        public string? Value { get; set; }
    }

    // ── Zone deserialization shapes ───────────────────────────────────────

    private sealed class ZoneNodeDto
    {
        public string Id { get; set; } = "";
        public string Type { get; set; } = "drop-zone";
        public string Name { get; set; } = "";
        public string Label { get; set; } = "";
        public int SortOrder { get; set; }
        // html-element fields
        public string? Tag { get; set; }
        public string? CssClass { get; set; }
        public Dictionary<string, string>? HtmlAttributes { get; set; }
        public List<ZoneNodeDto>? Children { get; set; }
        // legacy grid-row
        public List<ColumnDefDto>? Columns { get; set; }
    }

    private sealed class ColumnDefDto
    {
        public int Span { get; set; }
        public string ZoneName { get; set; } = "";
    }
}
