using Hellang.Middleware.ProblemDetails;
using MicroCMS.Infrastructure.Tenancy;

namespace MicroCMS.WebHost.Extensions;

internal static class ApplicationBuilderExtensions
{
    internal static WebApplication UseSecurityMiddleware(this WebApplication app)
    {
        if (!app.Environment.IsDevelopment())
        {
            app.UseHsts();
            app.UseHttpsRedirection();
        }

        app.UseCors();
        app.UseRateLimiter();

        // Correlation ID + security headers
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

        // Sprint 5 — resolve tenant from subdomain early in the pipeline
        app.UseMiddleware<TenantResolutionMiddleware>();

        return app;
    }

    internal static WebApplication UseApiMiddleware(this WebApplication app)
    {
        app.UseProblemDetails();

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapControllers();

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI(opt =>
            {
                opt.SwaggerEndpoint("/swagger/v1/swagger.json", "MicroCMS API v1");
                opt.RoutePrefix = "swagger";
            });
        }

        return app;
    }

    internal static WebApplication UseGraphQlMiddleware(this WebApplication app)
    {
        // TODO: Hot Chocolate endpoint (Sprint 9)
        return app;
    }

    internal static WebApplication UseHealthCheckEndpoints(this WebApplication app)
    {
        app.MapHealthChecks("/health/live");
        app.MapHealthChecks("/health/ready");
        return app;
    }
}
