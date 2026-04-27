using System.Reflection;
using FluentValidation;
using MediatR;
using MicroCMS.Application.Common.Authorization;
using MicroCMS.Application.Common.Behaviors;
using MicroCMS.Application.Common.Interfaces;
using MicroCMS.Application.Features.Components.Services;
using MicroCMS.Application.Features.Layouts.Services;
using Microsoft.Extensions.DependencyInjection;

namespace MicroCMS.Application;

/// <summary>
/// Registers all Application-layer services with the DI container.
/// Call <c>services.AddApplication()</c> from the Composition Root (API project).
///
/// Pipeline order (outer → inner):
///   Logging → Authorization → Validation → UnitOfWork → Handler
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(assembly);

            // Behaviors execute in registration order (first registered = outermost wrapper).
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(AuthorizationBehavior<,>));
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(UnitOfWorkBehavior<,>));
        });

        services.AddValidatorsFromAssembly(assembly, includeInternalTypes: true);

        services.AddScoped<IApplicationAuthorizationService, DefaultApplicationAuthorizationService>();
        services.AddScoped<LayoutShellGeneratorService>();
        services.AddScoped<ComponentBackingTypeProvisioner>();

        return services;
    }
}
