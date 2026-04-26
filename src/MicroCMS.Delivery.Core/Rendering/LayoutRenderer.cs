using HandlebarsDotNet;
using MicroCMS.Domain.Aggregates.Components;
using Microsoft.Extensions.Logging;

namespace MicroCMS.Delivery.Core.Rendering;

/// <summary>
/// Injects rendered zone HTML into a <see cref="Layout"/> shell template,
/// replacing <c>{{zone:name}}</c> and <c>{{seo:*}}</c> placeholder tokens.
///
/// Zone placeholder syntax (all template types):
///   <c>{{zone:hero-zone}}</c>  →  replaced with the accumulated HTML of that zone.
///
/// SEO placeholders:
///   <c>{{seo:title}}</c>  <c>{{seo:description}}</c>  <c>{{seo:ogImage}}</c>
/// </summary>
public interface ILayoutRenderer
{
    /// <summary>
    /// Renders the layout shell with the supplied zone HTML and SEO values.
    /// </summary>
    /// <param name="layout">The layout containing the shell template.</param>
    /// <param name="zones">Dictionary of zone-name → rendered HTML fragment.</param>
    /// <param name="seoTitle">Optional page title injected into <c>{{seo:title}}</c>.</param>
    /// <param name="seoDescription">Optional meta description.</param>
    /// <param name="seoOgImage">Optional OpenGraph image URL.</param>
Task<string> RenderAsync(
        Layout layout,
        IReadOnlyDictionary<string, string> zones,
        string? seoTitle= null,
string? seoDescription   = null,
        string? seoOgImage       = null,
        CancellationToken cancellationToken = default);
}

internal sealed class LayoutRenderer(ILogger<LayoutRenderer> logger) : ILayoutRenderer
{
    public Task<string> RenderAsync(
        Layout layout,
  IReadOnlyDictionary<string, string> zones,
        string? seoTitle       = null,
 string? seoDescription = null,
        string? seoOgImage     = null,
     CancellationToken cancellationToken = default)
  {
        if (string.IsNullOrWhiteSpace(layout.ShellTemplate))
          return Task.FromResult(FallbackZoneComment(zones));

        var html = layout.TemplateType switch
        {
      LayoutTemplateType.Handlebars => RenderHandlebars(layout, zones, seoTitle, seoDescription, seoOgImage),
  LayoutTemplateType.Html       => RenderTokenReplace(layout, zones, seoTitle, seoDescription, seoOgImage),
            _   => RenderHandlebars(layout, zones, seoTitle, seoDescription, seoOgImage),
        };

        return Task.FromResult(html);
    }

    // ── Handlebars ────────────────────────────────────────────────────────

    private string RenderHandlebars(
        Layout layout,
        IReadOnlyDictionary<string, string> zones,
        string? seoTitle, string? seoDescription, string? seoOgImage)
    {
  try
    {
    var template = Handlebars.Compile(layout.ShellTemplate!);
var data = BuildHandlebarsData(zones, seoTitle, seoDescription, seoOgImage);
  return template(data);
        }
        catch (Exception ex)
        {
   logger.LogError(ex, "Handlebars layout render failed for layout {Key}", layout.Key);
        return FallbackZoneComment(zones);
        }
    }

    private static Dictionary<string, object?> BuildHandlebarsData(
   IReadOnlyDictionary<string, string> zones,
        string? seoTitle, string? seoDescription, string? seoOgImage)
  {
        var data = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

     // Expose zones as  zone_hero_zone, zone_content_zone etc. (hyphens → underscores)
        // AND as a nested "zones" object so templates can use {{zone.hero-zone}} too.
        var zonesObj = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        foreach (var (name, html) in zones)
        {
            var safeKey = name.Replace('-', '_').Replace(' ', '_');
        data[$"zone_{safeKey}"] = (object)html;
  zonesObj[name]          = (object)html;
        }
        data["zones"] = zonesObj;

        // SEO
     var seo = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
 ["title"]       = seoTitle,
            ["description"] = seoDescription,
    ["ogImage"]     = seoOgImage,
    };
        data["seo"]  = seo;
        data["title"] = seoTitle;

        return data;
    }

    // ── Simple token replacement (Html / Razor fallback) ──────────────────

private static string RenderTokenReplace(
        Layout layout,
        IReadOnlyDictionary<string, string> zones,
    string? seoTitle, string? seoDescription, string? seoOgImage)
    {
        var shell = layout.ShellTemplate!;

        foreach (var (name, html) in zones)
            shell = shell.Replace($"{{{{zone:{name}}}}}", html, StringComparison.OrdinalIgnoreCase);

     shell = shell
   .Replace("{{seo:title}}",       seoTitle       ?? string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("{{seo:description}}", seoDescription ?? string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("{{seo:ogImage}}",     seoOgImage     ?? string.Empty, StringComparison.OrdinalIgnoreCase);

        return shell;
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private static string FallbackZoneComment(IReadOnlyDictionary<string, string> zones) =>
        string.Concat(zones.Select(z => $"<!-- zone:{z.Key} -->{z.Value}"));
}
