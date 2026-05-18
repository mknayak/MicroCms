using Scriban;
using Scriban.Runtime;
using MicroCMS.Application.Features.Delivery.Rendering;
using MicroCMS.Application.Features.Delivery.Rendering.Resolvers;
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
/// For Scriban layouts, zones are also directly available as
///   <c>{{ zone_hero_zone }}</c> (hyphens replaced with underscores).
///
/// SEO placeholders:
///   <c>{{seo:title}}</c>  <c>{{seo:description}}</c>  <c>{{seo:ogImage}}</c>
/// </summary>
public interface ILayoutRenderer
{
    /// <summary>
    /// Renders the layout shell with the supplied zone HTML, SEO values, and resolved tokens.
    /// </summary>
    /// <param name="layout">The layout containing the shell template.</param>
    /// <param name="zones">Dictionary of zone-name → rendered HTML fragment.</param>
    /// <param name="renderContext">Per-request render data used by the token resolution pipeline.</param>
    /// <param name="seoTitle">Optional page title injected into <c>{{seo:title}}</c>.</param>
    /// <param name="seoDescription">Optional meta description.</param>
    /// <param name="seoOgImage">Optional OpenGraph image URL.</param>
    Task<string> RenderAsync(
        Layout layout,
        IReadOnlyDictionary<string, string> zones,
        RenderContext renderContext,
        string? seoTitle = null,
        string? seoDescription = null,
        string? seoOgImage = null,
        CancellationToken cancellationToken = default);
}

internal sealed class LayoutRenderer(
    TokenResolutionPipeline tokenPipeline,
    ILogger<LayoutRenderer> logger) : ILayoutRenderer
{
    public async Task<string> RenderAsync(
        Layout layout,
        IReadOnlyDictionary<string, string> zones,
        RenderContext renderContext,
        string? seoTitle = null,
        string? seoDescription = null,
        string? seoOgImage = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(layout.ShellTemplate))
            return FallbackZoneComment(zones);

        // ── 1. Inject zone HTML into the shell ────────────────────────────
        var zoneResolved = layout.TemplateType switch
        {
            LayoutTemplateType.Scriban => RenderScriban(layout, zones, seoTitle, seoDescription, seoOgImage),
            LayoutTemplateType.Html => RenderTokenReplace(layout, zones, seoTitle, seoDescription, seoOgImage),
            _ => RenderScriban(layout, zones, seoTitle, seoDescription, seoOgImage),
        };

        // ── 2. Resolve remaining {{namespace:key}} tokens ─────────────────
        var html = await tokenPipeline.ResolveAsync(zoneResolved, renderContext, cancellationToken);

        return html;
    }

    // ── Scriban ───────────────────────────────────────────────────────────

    private string RenderScriban(
        Layout layout,
        IReadOnlyDictionary<string, string> zones,
        string? seoTitle, string? seoDescription, string? seoOgImage)
    {
        try
        {
            var processed = ScribanHelpers.Stash(layout.ShellTemplate!, out var stash);
            var template = Template.Parse(processed);
            if (template.HasErrors)
            {
                logger.LogError("Scriban parse errors for layout {Key}: {Errors}",
                    layout.Key, string.Join("; ", template.Messages));
                return FallbackZoneComment(zones);
            }

            var ctx = new TemplateContext { StrictVariables = false };
            ctx.PushGlobal(BuildScribanData(zones, seoTitle, seoDescription, seoOgImage));

            var result = template.Render(ctx);
            return ScribanHelpers.Restore(result, stash);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Scriban layout render failed for layout {Key}", layout.Key);
            return FallbackZoneComment(zones);
        }
    }

    private static ScriptObject BuildScribanData(
   IReadOnlyDictionary<string, string> zones,
        string? seoTitle, string? seoDescription, string? seoOgImage)
    {
        var scriptObj = new ScriptObject();

        // Expose zones as zone_hero_zone, zone_content_zone etc. (hyphens → underscores)
        // AND as a nested "zones" object so templates can use {{ zones.hero_zone }} too.
        var zonesObj = new ScriptObject();
        foreach (var (name, html) in zones)
        {
            var safeKey = name.Replace('-', '_').Replace(' ', '_');
            if (safeKey.StartsWith("zone_", StringComparison.OrdinalIgnoreCase))
                safeKey = safeKey["zone_".Length..];
            scriptObj[$"zone_{safeKey}"] = html;
            zonesObj[safeKey] = html;
        }
        scriptObj["zones"] = zonesObj;

        // SEO
        var seo = new ScriptObject();
        seo["title"] = seoTitle;
        seo["description"] = seoDescription;
        seo["og_image"] = seoOgImage;
        scriptObj["seo"] = seo;
        scriptObj["title"] = seoTitle;

        return scriptObj;
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
      .Replace("{{seo:title}}", seoTitle ?? string.Empty, StringComparison.OrdinalIgnoreCase)
               .Replace("{{seo:description}}", seoDescription ?? string.Empty, StringComparison.OrdinalIgnoreCase)
               .Replace("{{seo:ogImage}}", seoOgImage ?? string.Empty, StringComparison.OrdinalIgnoreCase);

        return shell;
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private static string FallbackZoneComment(IReadOnlyDictionary<string, string> zones) =>
        string.Concat(zones.Select(z => $"<!-- zone:{z.Key} -->{z.Value}"));
}
