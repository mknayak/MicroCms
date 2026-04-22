using Yarp.ReverseProxy.Configuration;

namespace MicroCMS.Admin.WebHost.Extensions;

/// <summary>
/// Registers a YARP reverse proxy that forwards all /api and /swagger requests
/// from the Admin WebHost to the MicroCMS.WebHost API project.
///
/// This means only one browser origin (Admin WebHost) is needed — CORS issues
/// between the SPA and the API are eliminated because the proxy handles forwarding
/// server-side.
/// </summary>
internal static class ApiProxyExtensions
{
    private const string ApiClusterId = "microcms-api";
    private const string ApiRouteId = "api-route";
    private const string SwaggerRouteId = "swagger-route";

    internal static WebApplicationBuilder AddAdminApiProxy(this WebApplicationBuilder builder)
    {
        var apiBaseUrl = builder.Configuration["ApiProxy:BaseUrl"]
       ?? "https://localhost:54188";

        builder.Services
            .AddReverseProxy()
 .LoadFromMemory(
          routes: BuildRoutes(),
          clusters: BuildClusters(apiBaseUrl));

    return builder;
    }

  internal static WebApplication UseAdminApiProxy(this WebApplication app)
    {
        app.MapReverseProxy();
        return app;
 }

    private static IReadOnlyList<RouteConfig> BuildRoutes() =>
    [
        new RouteConfig
        {
     RouteId = ApiRouteId,
     ClusterId = ApiClusterId,
      Match = new RouteMatch { Path = "/api/{**catch-all}" },
 },
        new RouteConfig
   {
            RouteId = SwaggerRouteId,
 ClusterId = ApiClusterId,
     Match = new RouteMatch { Path = "/swagger/{**catch-all}" },
        },
    ];

    private static IReadOnlyList<ClusterConfig> BuildClusters(string apiBaseUrl) =>
    [
        new ClusterConfig
        {
       ClusterId = ApiClusterId,
        HttpClient = new HttpClientConfig { DangerousAcceptAnyServerCertificate = true },
     Destinations = new Dictionary<string, DestinationConfig>
            {
    ["primary"] = new DestinationConfig { Address = apiBaseUrl },
            },
      },
    ];
}
