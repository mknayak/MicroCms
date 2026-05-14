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
using MicroCMS.Application.Features.Delivery.Services;
using MicroCMS.Application.Features.Layouts.Services;
using MicroCMS.Application.Features.Media.Options;
using Microsoft.Extensions.DependencyInjection;

namespace MicroCMS.Application;

/// <summary>
/// Core Application-layer DI extensions.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers MediatR, FluentValidation, pipeline behaviors, shared application
    /// services, and the token-resolution pipeline.
    ///
    /// Does NOT register <see cref="IPageRenderStep"/> implementations — each host
    /// project composes the steps it needs (see <see cref="AddDeliveryRenderSteps"/>).
    /// </summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Register MediaOptions with defaults; hosts override via
        // services.Configure<MediaOptions>(configuration.GetSection(MediaOptions.SectionName))
        services.AddOptions<MediaOptions>();

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
        services.AddScoped<EntryFieldExpander>();

        // Token resolution pipeline — resolvers ordered by execution priority.
        // SiteTokenResolver depends on ISettingsReader (Infrastructure), registered after AddInfrastructure().
        services.AddScoped<ITokenResolver, PageTokenResolver>();
        services.AddScoped<ITokenResolver, TemplateTokenResolver>();
        services.AddScoped<ITokenResolver, SeoTokenResolver>();
        services.AddScoped<ITokenResolver, SiteTokenResolver>();
        services.AddScoped<ITokenResolver, UserTokenResolver>();
        services.AddScoped<TokenResolutionPipeline>();

        // The pipeline executor — always needed; steps are registered by the host.
        services.AddScoped<PageRenderPipeline>();

        return services;
    }

    /// <summary>
    /// Registers the eight core page-render steps in execution order.
    ///
    /// Call this from any host that needs to render pages. The host controls
    /// whether to wrap these steps with cache steps:
    /// <code>
    /// services.AddDeliveryCacheReadStep();  // outermost: short-circuit on hit
    /// services.AddDeliveryRenderSteps();
    /// services.AddDeliveryCacheWriteStep(); // innermost: persist rendered HTML
    /// </code>
    /// Omit the cache registrations in hosts where stale output is unacceptable
    /// (e.g. the admin host serving editor preview requests).
    /// </summary>
    public static IServiceCollection AddPageRenderSteps(this IServiceCollection services)
    {
        services.AddScoped<IPageRenderStep, ResolvePageStep>();        // 1. slug → Page aggregate
        services.AddScoped<IPageRenderStep, ResolveSiteTemplateStep>(); // 2. SiteTemplate hierarchy
        services.AddScoped<IPageRenderStep, ResolvePageTemplateStep>(); // 3. PageTemplate placements
        services.AddScoped<IPageRenderStep, ResolveLayoutStep>();       // 4. Layout shell
        services.AddScoped<IPageRenderStep, ResolveSeoStep>();          // 5. SEO values
        services.AddScoped<IPageRenderStep, ResolvePageFieldsStep>();   // 6. page:* token fields
        services.AddScoped<IPageRenderStep, RenderZonesStep>();         // 7. component → zone HTML
        services.AddScoped<IPageRenderStep, RenderLayoutStep>();        // 8. zones → full HTML doc
        return services;
    }

    /// <summary>
    /// Registers the cache-read and cache-write steps that wrap the core render steps.
    /// </summary>
    public static IServiceCollection AddCachePageRenderingStep(this IServiceCollection services)
    {
        services.AddScoped<IPageRenderStep, ResolveCacheStep>();
        AddPageRenderSteps(services); // ensure core steps are registered after the cache step
        services.AddScoped<IPageRenderStep, WriteCacheStep>();

        return services;
    }

}
