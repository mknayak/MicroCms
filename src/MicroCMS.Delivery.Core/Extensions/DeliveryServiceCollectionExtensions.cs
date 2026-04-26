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
    /// <param name="setAsDefaultScheme">
    /// <c>true</c> (default) — use in <c>Delivery.WebHost</c> where API-key is the only auth scheme.
    /// <c>false</c> — use in hosts that already have a default scheme (e.g. JWT in <c>WebHost</c>);
    /// the API-key scheme is added as a secondary scheme without overwriting the existing default.
    /// </param>
    public static IServiceCollection AddDeliveryServices(
     this IServiceCollection services,
        bool setAsDefaultScheme = true)
    {
      // ── Authentication: API key scheme ────────────────────────────────
 var authBuilder = setAsDefaultScheme
   ? services.AddAuthentication(DeliveryApiKeyDefaults.SchemeName)
  : services.AddAuthentication(); // do NOT change the existing default

        authBuilder.AddScheme<DeliveryApiKeyOptions, DeliveryApiKeyHandler>(
            DeliveryApiKeyDefaults.SchemeName,
            _ => { });

    // ── Authorization: require the api_key auth_method claim ──────────
services.AddAuthorization(opt =>
        {
 if (setAsDefaultScheme)
  {
      // Delivery.WebHost: API key is the only scheme, set as default policy.
    opt.DefaultPolicy = new AuthorizationPolicyBuilder(DeliveryApiKeyDefaults.SchemeName)
   .RequireAuthenticatedUser()
     .RequireClaim("auth_method", "api_key")
                    .Build();
            }
            // In WebHost the default policy is already set to JWT by AddSecurityServices();
   // only add the named policy so delivery controllers can reference it explicitly.

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
