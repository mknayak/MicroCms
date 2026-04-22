using Microsoft.AspNetCore.SpaServices.Extensions;

namespace MicroCMS.Admin.WebHost.Extensions;

internal static class SpaExtensions
{
    private const string SpaSourcePath = "ClientApp";
    private const string ViteDevServerUrl = "http://localhost:5174";

    internal static WebApplicationBuilder AddAdminSpaServices(this WebApplicationBuilder builder)
    {
        builder.Services.AddSpaStaticFiles(configuration =>
        {
            configuration.RootPath = $"{SpaSourcePath}/dist";
     });

        return builder;
    }

    internal static WebApplication UseAdminSpa(this WebApplication app)
    {
      var viteRunning = app.Environment.IsDevelopment() && IsViteRunning();

        // Serve static files from dist/ whenever Vite is not proxying
        if (!viteRunning)
        {
    app.UseSpaStaticFiles();
      }

        app.UseSpa(spa =>
        {
            spa.Options.SourcePath = SpaSourcePath;

            if (viteRunning)
   {
        spa.UseProxyToSpaDevelopmentServer(ViteDevServerUrl);
    }
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
