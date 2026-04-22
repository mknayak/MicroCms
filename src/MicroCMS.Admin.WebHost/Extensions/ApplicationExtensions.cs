using MicroCMS.Application;
using MicroCMS.Infrastructure;

namespace MicroCMS.Admin.WebHost.Extensions;

/// <summary>
/// Registers application-layer services (MediatR, validators) needed by the Admin BFF.
/// </summary>
internal static class ApplicationExtensions
{
    internal static WebApplicationBuilder AddAdminApplicationServices(this WebApplicationBuilder builder)
    {
        builder.Services.AddApplication();
        return builder;
    }
    internal static WebApplicationBuilder AddInfrastructureServices(this WebApplicationBuilder builder)
    {
        builder.Services.AddInfrastructure(builder.Configuration);
        return builder;
    }
}
