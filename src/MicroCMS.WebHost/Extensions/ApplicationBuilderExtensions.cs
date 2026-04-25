using Hellang.Middleware.ProblemDetails;
using MicroCMS.Api.Middleware;
using MicroCMS.Infrastructure.Install;
using MicroCMS.Infrastructure.Persistence.Common;
using MicroCMS.Infrastructure.Tenancy;
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
    /// Applies the database schema on startup.
 /// - SQLite / development: <c>EnsureCreated</c> (fast, no migrations needed).
    /// - PostgreSQL / production: <c>MigrateAsync</c> (runs pending EF Core migrations).
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
        else
        {
            // For real databases apply pending migrations (safe to call even when up-to-date).
  await db.Database.MigrateAsync();
      }
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

        return app;
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
