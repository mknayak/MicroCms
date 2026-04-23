using Hellang.Middleware.ProblemDetails;
using MicroCMS.Api.Middleware;
using MicroCMS.Infrastructure.Install;
using MicroCMS.Infrastructure.Tenancy;

namespace MicroCMS.WebHost.Extensions;

/// <summary>
/// <see cref="WebApplication"/> pipeline-level extension methods for the MicroCMS composition root.
/// Each method handles one vertical concern; cyclomatic complexity per method stays low.
/// </summary>
internal static class ApplicationBuilderExtensions
{
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
                opt.RoutePrefix = "api-docs";
            });
        }

        app.MapControllers();

        return app;
    }

    // ── GraphQL middleware ────────────────────────────────────────────────

    internal static WebApplication UseGraphQlMiddleware(this WebApplication app)
    {
        // TODO Sprint 9 – map Hot Chocolate GraphQL endpoint
        // app.MapGraphQL("/graphql");
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
