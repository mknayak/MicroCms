using Microsoft.Extensions.FileProviders;
using Microsoft.AspNetCore.SpaServices.Extensions;

namespace MicroCMS.Admin.WebHost.Extensions;

internal static class SpaExtensions
{
    private const string SpaSourcePath = "ClientApp";
    private const string ViteDevServerUrl = "http://localhost:5174";

    internal static WebApplicationBuilder AddAdminSpaServices(this WebApplicationBuilder builder)
    {
        // AddSpaStaticFiles is only needed for the legacy UseSpa path (dev proxy).
        // The production path uses UseStaticFiles directly, so this is a no-op there.
        builder.Services.AddSpaStaticFiles(configuration =>
        {
            configuration.RootPath = $"{SpaSourcePath}/dist";
        });

        return builder;
    }

    internal static WebApplication UseAdminSpa(this WebApplication app)
    {
        var viteRunning = app.Environment.IsDevelopment() && IsViteRunning();

        if (viteRunning)
        {
            // Development with Vite running — proxy all traffic to the dev server.
            app.UseSpa(spa =>
            {
                spa.Options.SourcePath = SpaSourcePath;
                spa.UseProxyToSpaDevelopmentServer(ViteDevServerUrl);
            });

            return app;
        }

        // Production build (or development without Vite running).
        //
        // UseStaticFiles + MapFallbackToFile is the correct pattern for Vite production
        // builds.  The legacy UseSpaStaticFiles + UseSpa combo has a well-known race
        // where UseSpa() intercepts hashed chunk requests (/assets/*.js) before
        // UseSpaStaticFiles() can serve them, returning index.html (text/html) in place
        // of JavaScript and causing "Failed to fetch dynamically imported module".
        var distPath = Path.Combine(
                app.Environment.ContentRootPath, SpaSourcePath, "dist");

        if (!Directory.Exists(distPath))
        {
            // Warn loudly rather than silently falling through to a 404 on every request.
            app.Logger.LogWarning(
        "SPA dist folder not found at '{DistPath}'. " +
        "Run 'npm run build' inside {SpaSourcePath} before starting in production mode.",
      distPath, SpaSourcePath);
        }

        var fileProvider = new PhysicalFileProvider(distPath);

        // Serve hashed assets (JS, CSS, images, fonts) from dist/assets/.
        app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = fileProvider,
            RequestPath  = string.Empty,
        });

        // For every route that does not match a static file or an API endpoint,
        // return index.html so the React router can handle it client-side.
        app.MapFallbackToFile("index.html", new StaticFileOptions
        {
            FileProvider = fileProvider,
        });

        return app;
    }

    /// <summary>Quick TCP probe to check whether the Vite dev server is listening.</summary>
    private static bool IsViteRunning()
    {
        try
        {
            using var client = new System.Net.Sockets.TcpClient();
            client.Connect("localhost", 5174);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
