using System.Text;
using System.Text.Json;

namespace MicroCMS.Application.Features.Layouts.Services;

/// <summary>
/// Generates a Handlebars or HTML shell template from a structured zone tree.
/// Called by layout command handlers whenever zones change.
/// </summary>
public sealed class LayoutShellGeneratorService
{
    private static readonly JsonSerializerOptions _json = new() { PropertyNameCaseInsensitive = true };

 public string Generate(string zonesJson, string templateType)
    {
  var zones = JsonSerializer.Deserialize<List<ZoneNodeDto>>(zonesJson, _json) ?? [];
        var isHandlebars = !templateType.Equals("Html", StringComparison.OrdinalIgnoreCase);
 return isHandlebars ? BuildHandlebars(zones) : BuildHtml(zones);
    }

  private static string BuildHandlebars(List<ZoneNodeDto> zones)
    {
        var sb = new StringBuilder();
      sb.AppendLine("<!DOCTYPE html>");
 sb.AppendLine("<html>");
        sb.AppendLine("<head>");
  sb.AppendLine("  <title>{{seo_title}}</title>");
  sb.AppendLine("  <meta name=\"description\" content=\"{{seo_description}}\">");
        sb.AppendLine("</head>");
      sb.AppendLine("<body>");

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

        sb.AppendLine("</body>");
     sb.AppendLine("</html>");
   return sb.ToString();
    }

    private static string BuildHtml(List<ZoneNodeDto> zones)
    {
     var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html>");
      sb.AppendLine("<head>");
        sb.AppendLine("  <title>{{seo:title}}</title>");
        sb.AppendLine("  <meta name=\"description\" content=\"{{seo:description}}\">");
        sb.AppendLine("</head>");
        sb.AppendLine("<body>");

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

     sb.AppendLine("</body>");
    sb.AppendLine("</html>");
        return sb.ToString();
    }

    // DTOs for deserialization only
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
