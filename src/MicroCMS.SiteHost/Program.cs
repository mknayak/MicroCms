// MicroCMS.SiteHost — multi-site YARP reverse proxy.
//
// One running instance handles all configured hostnames.
// The incoming Host header selects the correct site_id / api_key at request time.
//
// Flow:
//   Browser  GET  site-a.com/about-us
//    → SiteResolver matches "site-a.com" → SiteEntry { SiteId, ApiKey, Locale }
//    → YARP rewrites path  → /api/delivery/pages/about-us/render
//    → injects ?siteId=...&locale=...  +  X-Api-Key header
//    → returns HTML directly to browser
//
// Configuration — appsettings.json SiteHost section:
//   DeliveryBaseUrl  – shared backend URL (single source of truth)
//   Sites[]          – one entry per hostname: Hostname / SiteId / ApiKey / DefaultLocale

using MicroCMS.SiteHost;
using Serilog;
using Yarp.ReverseProxy.Configuration;
using Yarp.ReverseProxy.Transforms;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true)
    .AddEnvironmentVariables();

// ── Serilog ───────────────────────────────────────────────────────────────────
builder.Host.UseSerilog((ctx, lc) => lc
    .ReadFrom.Configuration(ctx.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console(formatProvider: System.Globalization.CultureInfo.InvariantCulture));

// ── Site configuration ────────────────────────────────────────────────────────
builder.Services
    .AddOptions<SiteHostOptions>()
    .BindConfiguration(SiteHostOptions.Section)
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddSingleton<SiteResolver>();

// ── YARP ──────────────────────────────────────────────────────────────────────
// Routes come from appsettings.json (ReverseProxy:Routes).
// Cluster destination is built from SiteHost:DeliveryBaseUrl — single source of truth.
var deliveryBase = builder.Configuration[$"{SiteHostOptions.Section}:DeliveryBaseUrl"]
    ?? throw new InvalidOperationException("SiteHost:DeliveryBaseUrl is required.");

var routes = builder.Configuration
    .GetSection("ReverseProxy:Routes")
    .Get<Dictionary<string, RouteConfig>>()!
    .Select(kv => kv.Value with { RouteId = kv.Key })
  .ToList();

var clusters = new List<ClusterConfig>
{
    new()
    {
        ClusterId = "delivery",
        Destinations = new Dictionary<string, DestinationConfig>
        {
            ["primary"] = new() { Address = deliveryBase }
      }
    }
};

builder.Services
    .AddReverseProxy()
    .LoadFromMemory(routes, clusters)
    .AddTransforms(ctx =>
    {
        if (!string.Equals(ctx.Route.ClusterId, "delivery", StringComparison.OrdinalIgnoreCase))
       return;

  // Per-request transform: resolve the site from the Host header.
     // The lambda is called on every request, so SiteResolver is called each time.
        ctx.AddRequestTransform(async requestCtx =>
        {
            var resolver = requestCtx.HttpContext.RequestServices
    .GetRequiredService<SiteResolver>();

            var site = resolver.Resolve(requestCtx.HttpContext);

         if (site is null)
        {
         // Unknown hostname — reject immediately
                requestCtx.HttpContext.Response.StatusCode = StatusCodes.Status421MisdirectedRequest;
   await requestCtx.HttpContext.Response
   .WriteAsync($"Host '{requestCtx.HttpContext.Request.Host.Host}' is not configured.")
        .ConfigureAwait(false);
          return;
        }

    // 1. Inject the API key — never exposed to the browser
          requestCtx.ProxyRequest.Headers.Remove("X-Api-Key");
     requestCtx.ProxyRequest.Headers.TryAddWithoutValidation("X-Api-Key", site.ApiKey);

     // 2. Append siteId + locale via YARP's query builder.
      //    RequestUri is null here — YARP builds it from Path + Query after transforms.
  //    Use requestCtx.Query.Collection; YARP assembles the final URI afterwards.
            requestCtx.Query.Collection["siteId"] = site.SiteId;
    requestCtx.Query.Collection["locale"]  = site.DefaultLocale;

            // 3. Request full HTML from the delivery render endpoint
         requestCtx.ProxyRequest.Headers.Remove("Accept");
            requestCtx.ProxyRequest.Headers.TryAddWithoutValidation("Accept", "text/html");
    });
    });

// ── Health checks ─────────────────────────────────────────────────────────────
builder.Services.AddHealthChecks();

// ─────────────────────────────────────────────────────────────────────────────
var app = builder.Build();

app.UseSerilogRequestLogging();
app.UseStaticFiles();
app.MapHealthChecks("/health");
app.MapReverseProxy();

await app.RunAsync().ConfigureAwait(false);
