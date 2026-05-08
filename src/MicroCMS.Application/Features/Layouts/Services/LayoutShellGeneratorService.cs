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
        {
            if (zone.Type == "grid-row" && zone.Columns?.Count > 0)
            {
                sb.AppendLine($"  <div class=\"grid-row\" data-zone-row=\"{zone.Name}\">");
                foreach (var col in zone.Columns)
                {
                    var token = col.ZoneName.Replace("-", "_");
                    sb.AppendLine($"    <div class=\"col-{col.Span}\" data-zone=\"{col.ZoneName}\">");
                    sb.AppendLine($"      {{{{{{{token}}}}}}}");
                    sb.AppendLine("    </div>");
                }
                sb.AppendLine("  </div>");
            }
            else
            {
                var token = zone.Name.Replace("-", "_");
                sb.AppendLine($"  <div data-zone=\"{zone.Name}\">");
                sb.AppendLine($"    {{{{{{{token}}}}}}}");
                sb.AppendLine("  </div>");
            }
        }

        AppendBodyEndAssets(sb, config, "  ");
        sb.AppendLine("</body>");
        sb.AppendLine("</html>");
        return sb.ToString();
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
        {
            if (zone.Type == "grid-row" && zone.Columns?.Count > 0)
            {
                sb.AppendLine($"  <div class=\"grid-row\" data-zone-row=\"{zone.Name}\">");
                foreach (var col in zone.Columns)
                {
                    sb.AppendLine($"    <div class=\"col-{col.Span}\" data-zone=\"{col.ZoneName}\">");
                    sb.AppendLine($"      {{{{zone:{col.ZoneName}}}}}");
                    sb.AppendLine("    </div>");
                }
                sb.AppendLine("  </div>");
            }
            else
            {
                sb.AppendLine($"  <div data-zone=\"{zone.Name}\">");
                sb.AppendLine($"    {{{{zone:{zone.Name}}}}}");
                sb.AppendLine("  </div>");
            }
        }

        AppendBodyEndAssets(sb, config, "  ");
        sb.AppendLine("</body>");
        sb.AppendLine("</html>");
        return sb.ToString();
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
        public string Type { get; set; } = "zone";
        public string Name { get; set; } = "";
        public string Label { get; set; } = "";
        public int SortOrder { get; set; }
        public List<ColumnDefDto>? Columns { get; set; }
    }

    private sealed class ColumnDefDto
    {
        public int Span { get; set; }
        public string ZoneName { get; set; } = "";
    }
}
