using Hellang.Middleware.ProblemDetails;
using MicroCMS.Api.Middleware;
using MicroCMS.Application.Common.Interfaces;
using MicroCMS.Domain.Aggregates.Media;
using MicroCMS.Domain.Repositories;
using MicroCMS.Domain.Specifications.Delivery;
using MicroCMS.Infrastructure.Install;
using MicroCMS.Infrastructure.Persistence.Common;
using MicroCMS.Infrastructure.Tenancy;
using MicroCMS.Shared.Ids;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;

namespace MicroCMS.WebHost.Extensions;

/// <summary>
/// <see cref="WebApplication"/> pipeline-level extension methods for the MicroCMS composition root.
/// Each method handles one vertical concern; cyclomatic complexity per method stays low.
/// </summary>
internal static class ApplicationBuilderExtensions
{
    // ── Database initialisation ───────────────────────────────────────────

    /// <summary>
    /// Initialises the database connection on startup.
    /// - SQLite / development: <c>EnsureCreated</c> (fast, creates schema from the current model).
    /// - PostgreSQL / production: schema is managed via SQL scripts in
    ///   <c>Persistence/PostgreSql/Scripts/</c>; no automatic migration is run.
    /// </summary>
    internal static async Task UseDatabaseAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var config = app.Configuration;

        var provider = config.GetValue<string>("MicroCMS:Database:Provider") ?? "Sqlite";

        if (provider.Equals("Sqlite", StringComparison.OrdinalIgnoreCase))
        {
            // EnsureCreated is fine for SQLite dev — creates the schema from the current model.
            await db.Database.EnsureCreatedAsync();
        }
        // PostgreSQL schema is managed via SQL scripts; migrations are not applied at runtime.
    }

    // ── Security middleware ────────────────────────────────────────────────

    internal static WebApplication UseSecurityMiddleware(this WebApplication app)
    {
        if (!app.Environment.IsDevelopment())
        {
            app.UseHsts();
            app.UseHttpsRedirection();
        }

        app.UseCors();
        app.UseRateLimiter();

        // Installation guard — must run before auth so unauthenticated install requests
        // are allowed through, and all other requests are blocked with 503 until setup
        // is complete.
        app.UseMiddleware<InstallationGuardMiddleware>();

        // Correlation ID + baseline security response headers
        app.Use(async (ctx, next) =>
        {
            var correlationId = ctx.Request.Headers["X-Correlation-ID"].FirstOrDefault()
                ?? Guid.NewGuid().ToString();

            ctx.Response.Headers["X-Correlation-ID"] = correlationId;
            ctx.Response.Headers["X-Content-Type-Options"] = "nosniff";
            ctx.Response.Headers["X-Frame-Options"] = "DENY";
            ctx.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";

            await next();
        });

        // API key authentication must run before the standard auth pipeline
        // so it can synthesise a ClaimsPrincipal the authorization middleware sees.
        app.UseMiddleware<ApiKeyAuthenticationMiddleware>();

        app.UseAuthentication();
        app.UseAuthorization();

        // Multi-tenant resolution: extracts tenant from subdomain, sets ITenantContext
        app.UseMiddleware<TenantResolutionMiddleware>();

        return app;
    }

    // ── REST API middleware ────────────────────────────────────────────────

    internal static WebApplication UseApiMiddleware(this WebApplication app)
    {
        app.UseProblemDetails();

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI(opt =>
            {
                opt.SwaggerEndpoint("/swagger/v1/swagger.json", "MicroCMS API v1");
                opt.RoutePrefix = "swagger";
            });
        }

        app.MapControllers();
        app.MapMediaAssetsByPath();

        return app;
    }

    // ── Static asset path endpoint ────────────────────────────────────────

    /// <summary>
    /// Registers the anonymous <c>GET /static/assets/{**path}</c> endpoint.
    ///
    /// Resolves the virtual path to a <see cref="MediaAsset"/> and streams the
    /// binary from storage with a long-lived <c>Cache-Control: public, max-age=31536000</c>
    /// header so browsers cache layout CSS/JS files without hitting the GUID-based API.
    /// </summary>
    private static void MapMediaAssetsByPath(this WebApplication app)
    {
        app.MapGet("/static/assets/{**path}",
            async (string path,
            HttpRequest request,
            HttpResponse response,
            IRepository<MediaAsset, MediaAssetId> assetRepo,
            IStorageProvider storageProvider,
            CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(path))
                return Results.NotFound();

            // Resolve SiteId from the mcms_site cookie written by the admin SPA
            // when the user selects a site from the dropdown.
            if (!request.Cookies.TryGetValue("mcms_site", out var rawSiteId) ||
                !Guid.TryParse(rawSiteId, out var siteGuid))
                return Results.NotFound();

            var siteId = new SiteId(siteGuid);
            var spec = new AvailableMediaAssetByPathSpec(siteId, path.TrimStart('/'));
            var matches = await assetRepo.ListAsync(spec, ct);
            var asset = matches.FirstOrDefault();
            if (asset is null)
                return Results.NotFound();

            response.Headers.CacheControl = "public, max-age=31536000, immutable";

            // Prefer the stored MIME type; fall back to extension-based lookup so
            // browsers always receive a correct Content-Type (e.g. text/css, text/javascript).
            var mimeType = asset.Metadata.MimeType;
            if (string.IsNullOrWhiteSpace(mimeType))
            {
                var provider = new FileExtensionContentTypeProvider();
                if (!provider.TryGetContentType(path, out mimeType))
                    mimeType = "application/octet-stream";
            }

            var stream = await storageProvider.DownloadAsync(asset.StorageKey, ct);
            return Results.Stream(stream, contentType: mimeType, enableRangeProcessing: true);
        })
        .AllowAnonymous()
        .WithName("GetAssetByPath")
        .WithTags("Media");
    }

    // ── GraphQL middleware ────────────────────────────────────────────────

    internal static WebApplication UseGraphQlMiddleware(this WebApplication app)
    {
        // WebSockets must be enabled before MapGraphQL so subscription transports work.
        app.UseWebSockets();

  // Mount the Hot Chocolate endpoint at /graphql.
        // Banana Cake Pop (HC IDE) is served alongside in Development for interactive exploration.
        app.MapGraphQL("/graphql");

        if (app.Environment.IsDevelopment())
   {
    app.MapBananaCakePop("/graphql/ui");
   }

      return app;
    }

    // ── Health check endpoints ────────────────────────────────────────────

    internal static WebApplication UseHealthCheckEndpoints(this WebApplication app)
    {
        // Liveness: process-alive only (no dependency checks)
        app.MapHealthChecks("/health/live",
            new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
            {
                Predicate = _ => false,
            });

        // Readiness: all registered health checks
        app.MapHealthChecks("/health/ready",
            new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
            {
                Predicate = _ => true,
            });

        return app;
    }
}
