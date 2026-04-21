namespace MicroCMS.WebHost.Extensions;

internal static class ApplicationBuilderExtensions
{
    internal static WebApplication UseSecurityMiddleware(this WebApplication app)
    {
        // TODO: HSTS, HTTPS redirection, correlation ID, tenant resolution (Sprint 2)
        return app;
    }

    internal static WebApplication UseApiMiddleware(this WebApplication app)
    {
        // TODO: Problem details, routing, auth, controllers, Swagger (Sprint 2)
        return app;
    }

    internal static WebApplication UseGraphQlMiddleware(this WebApplication app)
    {
        // TODO: Hot Chocolate endpoint (Sprint 4)
        return app;
    }

    internal static WebApplication UseHealthCheckEndpoints(this WebApplication app)
    {
        // TODO: /health/live and /health/ready (Sprint 2)
        return app;
    }
}
