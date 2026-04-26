using MicroCMS.Application.Features.Delivery.Rendering;
using MicroCMS.Delivery.Core.Auth;
using MicroCMS.Delivery.Core.Rendering;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace MicroCMS.Delivery.Core.Extensions;

/// <summary>
/// Registers all Delivery-layer services: API-key authentication scheme,
/// delivery authorization policy, and component renderer.
///
/// Call <c>AddDeliveryServices()</c> from the Delivery host's composition root
/// <b>after</b> <c>AddInfrastructure()</c> so repositories are already registered.
/// </summary>
public static class DeliveryServiceCollectionExtensions
{
    public static IServiceCollection AddDeliveryServices(this IServiceCollection services)
    {
 // ── Authentication: API key scheme ────────────────────────────────
   services
          .AddAuthentication(DeliveryApiKeyDefaults.SchemeName)
        .AddScheme<DeliveryApiKeyOptions, DeliveryApiKeyHandler>(
         DeliveryApiKeyDefaults.SchemeName,
     _ => { });

        // ── Authorization: require the api_key auth_method claim ──────────
      services.AddAuthorization(opt =>
        {
     // Default policy: must be authenticated via an active API key.
       opt.DefaultPolicy = new AuthorizationPolicyBuilder(DeliveryApiKeyDefaults.SchemeName)
          .RequireAuthenticatedUser()
        .RequireClaim("auth_method", "api_key")
         .Build();

     // Named constant re-used by delivery controllers.
     opt.AddPolicy(DeliveryPolicies.ApiKeyAuthenticated,
       p => p
    .AddAuthenticationSchemes(DeliveryApiKeyDefaults.SchemeName)
         .RequireAuthenticatedUser()
    .RequireClaim("auth_method", "api_key"));
        });

        // ── Rendering ─────────────────────────────────────────────────────
        services.AddSingleton<IComponentRenderer, ComponentRenderer>();
   services.AddSingleton<ILayoutRenderer, LayoutRenderer>();
        services.AddSingleton<IComponentRenderingService, ComponentRenderingService>();

        return services;
    }
}

/// <summary>Named authorization policies used by the delivery layer.</summary>
public static class DeliveryPolicies
{
    public const string ApiKeyAuthenticated = "Delivery.ApiKeyAuthenticated";
}
