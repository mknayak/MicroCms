using System.Reflection;
using FluentValidation;
using MediatR;
using MicroCMS.Application.Common.Authorization;
using MicroCMS.Application.Common.Behaviors;
using MicroCMS.Application.Common.Interfaces;
using MicroCMS.Application.Features.Components.Services;
using MicroCMS.Application.Features.Delivery.Pipeline;
using MicroCMS.Application.Features.Delivery.Pipeline.Steps;
using MicroCMS.Application.Features.Delivery.Rendering;
using MicroCMS.Application.Features.Delivery.Rendering.Resolvers;
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

        // Token resolution pipeline — resolvers ordered by execution priority.
        // SiteTokenResolver depends on ISettingsReader (Infrastructure), registered after AddInfrastructure().
        services.AddScoped<ITokenResolver, PageTokenResolver>();
        services.AddScoped<ITokenResolver, TemplateTokenResolver>();
        services.AddScoped<ITokenResolver, SeoTokenResolver>();
        services.AddScoped<ITokenResolver, SiteTokenResolver>();
        services.AddScoped<ITokenResolver, UserTokenResolver>();
        services.AddScoped<TokenResolutionPipeline>();

        // Page render pipeline — steps execute in registration order (first = outermost).
        // To add a new rendering concern, implement IPageRenderStep and register it here
        // at the desired position. No existing step needs to change.
        services.AddScoped<IPageRenderStep, ResolveCacheStep>();       // 1. cache read (short-circuits on hit)
        services.AddScoped<IPageRenderStep, ResolvePageStep>();        // 2. slug → Page aggregate
        services.AddScoped<IPageRenderStep, ResolveSiteTemplateStep>(); // 3. SiteTemplate hierarchy
        services.AddScoped<IPageRenderStep, ResolvePageTemplateStep>(); // 4. PageTemplate placements
        services.AddScoped<IPageRenderStep, ResolveLayoutStep>();      // 5. Layout shell
        services.AddScoped<IPageRenderStep, ResolveSeoStep>();         // 6. SEO values
        services.AddScoped<IPageRenderStep, ResolvePageFieldsStep>();  // 7. page:* token fields
        services.AddScoped<IPageRenderStep, RenderZonesStep>();        // 8. component → zone HTML
        services.AddScoped<IPageRenderStep, RenderLayoutStep>();       // 9. zones → full HTML doc
        services.AddScoped<IPageRenderStep, WriteCacheStep>();         // 10. cache write
        services.AddScoped<PageRenderPipeline>();

        return services;
    }
}
