using Microsoft.AspNetCore.SpaServices.Extensions;

namespace MicroCMS.Admin.WebHost.Extensions;

/// <summary>
/// Registers and configures the Vite-powered React SPA.
/// In Development:  proxies to the Vite dev server (http://localhost:5174).
/// In Production:   serves the pre-built assets from ClientApp/dist/.
/// </summary>
internal static class SpaExtensions
{
    private const string SpaSourcePath = "ClientApp";
    private const string ViteDevServerUrl = "http://localhost:5174";

    internal static WebApplicationBuilder AddAdminSpaServices(this WebApplicationBuilder builder)
    {
        builder.Services.AddSpaStaticFiles(configuration =>
        {
            // Production: serve from the build output directory
            configuration.RootPath = $"{SpaSourcePath}/dist";
        });

        return builder;
    }

    internal static WebApplication UseAdminSpa(this WebApplication app)
    {
        // Serve static files from ClientApp/dist in production
        if (!app.Environment.IsDevelopment())
        {
            app.UseSpaStaticFiles();
        }

        app.UseSpa(spa =>
        {
            spa.Options.SourcePath = SpaSourcePath;

            if (app.Environment.IsDevelopment())
            {
                // Proxy all unmatched requests to the Vite dev server
                spa.UseProxyToSpaDevelopmentServer(ViteDevServerUrl);
            }
        });

        return app;
    }
}
