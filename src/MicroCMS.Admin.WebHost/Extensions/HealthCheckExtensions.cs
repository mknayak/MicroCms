namespace MicroCMS.Admin.WebHost.Extensions;

/// <summary>
/// Registers and maps health-check endpoints for the Admin host.
/// </summary>
internal static class HealthCheckExtensions
{
    internal static WebApplicationBuilder AddAdminHealthChecks(this WebApplicationBuilder builder)
    {
        builder.Services
            .AddHealthChecks()
            .AddCheck("self", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy());

        return builder;
    }
}
