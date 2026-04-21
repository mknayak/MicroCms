// Placeholder — concrete registrations implemented sprint-by-sprint.
// Each Add* method below corresponds to a layer or cross-cutting concern.
// Keeping them in separate methods limits cyclomatic complexity per method to ≤ 5.
namespace MicroCMS.WebHost.Extensions;

internal static class ServiceCollectionExtensions
{
    internal static WebApplicationBuilder AddLoggingAndTelemetry(this WebApplicationBuilder builder)
    {
        // TODO: Serilog + OpenTelemetry registration (Sprint 1)
        return builder;
    }

    internal static WebApplicationBuilder AddSecurityServices(this WebApplicationBuilder builder)
    {
        // TODO: JWT bearer, CORS, rate limiting, data protection (Sprint 2)
        return builder;
    }

    internal static WebApplicationBuilder AddApplicationServices(this WebApplicationBuilder builder)
    {
        // TODO: MediatR, FluentValidation, pipeline behaviors (Sprint 1)
        return builder;
    }

    internal static WebApplicationBuilder AddInfrastructureServices(this WebApplicationBuilder builder)
    {
        // TODO: EF Core, Redis, storage, search, outbox (Sprint 2)
        return builder;
    }

    internal static WebApplicationBuilder AddApiServices(this WebApplicationBuilder builder)
    {
        // TODO: MVC, versioning, Swagger, problem details (Sprint 2)
        return builder;
    }

    internal static WebApplicationBuilder AddGraphQlServices(this WebApplicationBuilder builder)
    {
        // TODO: Hot Chocolate dynamic schema (Sprint 4)
        return builder;
    }

    internal static WebApplicationBuilder AddPluginHosting(this WebApplicationBuilder builder)
    {
        // TODO: AssemblyLoadContext plugin loader (Sprint 5)
        return builder;
    }

    internal static WebApplicationBuilder AddAiServices(this WebApplicationBuilder builder)
    {
        // TODO: AI provider registry, orchestrator, budget service (Sprint 6)
        return builder;
    }

    internal static WebApplicationBuilder AddHealthChecks(this WebApplicationBuilder builder)
    {
        // TODO: DB, Redis, search health checks (Sprint 2)
        return builder;
    }
}
