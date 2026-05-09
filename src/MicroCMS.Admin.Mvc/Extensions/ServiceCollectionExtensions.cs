using MicroCMS.Admin.Mvc.Infrastructure;
using MicroCMS.Admin.Mvc.Services;
using MicroCMS.Admin.Mvc.Services.Abstractions;

namespace MicroCMS.Admin.Mvc.Extensions;

/// <summary>
/// Registers all application services, typed HttpClients, and MVC infrastructure.
/// </summary>
internal static class ServiceCollectionExtensions
{
    internal static WebApplicationBuilder AddAdminMvcServices(this WebApplicationBuilder builder)
    {
        builder.Services.AddControllersWithViews(options =>
        {
            // Global anti-forgery validation can be enforced here if desired
        });

        builder.Services.AddHttpContextAccessor();

        RegisterHttpClients(builder);
        RegisterApplicationServices(builder.Services);

        return builder;
    }

    private static void RegisterHttpClients(WebApplicationBuilder builder)
    {
        var apiBaseUrl = builder.Configuration["ApiProxy:BaseUrl"] ?? "https://localhost:54188";

        // Named HttpClient for the MicroCMS backend API.
        // The TokenDelegatingHandler attaches the JWT stored in the current user's claims.
        builder.Services
            .AddHttpClient(HttpClientNames.ApiClient, client =>
            {
                client.BaseAddress = new Uri(apiBaseUrl.TrimEnd('/') + "/api/v1/");
                client.DefaultRequestHeaders.Add("Accept", "application/json");
                client.Timeout = TimeSpan.FromSeconds(30);
            })
            .AddHttpMessageHandler<TokenDelegatingHandler>()
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
            {
                // Accept self-signed certs in development — remove in production
                ServerCertificateCustomValidationCallback =
                    HttpClientHandler.DangerousAcceptAnyServerCertificateValidator,
            });

        builder.Services.AddTransient<TokenDelegatingHandler>();
    }

    private static void RegisterApplicationServices(IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<IContentTypeService, ContentTypeService>();
        services.AddScoped<IEntryService, EntryService>();
        services.AddScoped<IMediaService, MediaService>();
        services.AddScoped<ITaxonomyService, TaxonomyService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IPageService, PageService>();
        services.AddScoped<ILayoutService, LayoutService>();
        services.AddScoped<IComponentService, ComponentService>();
        services.AddScoped<ITenantService, TenantService>();
        services.AddScoped<ISearchService, SearchService>();
        services.AddScoped<IPackageService, PackageService>();
        services.AddScoped<ISiteService, SiteService>();
    }
}

/// <summary>
/// Well-known HTTP client names used throughout the application.
/// </summary>
internal static class HttpClientNames
{
    internal const string ApiClient = "MicroCmsApi";
}
