using MicroCMS.Application.Common.Interfaces;
using MicroCMS.Application.Features.ApiClients.Commands;
using MicroCMS.Application.Features.Entries.Queries.Preview;
using MicroCMS.Application.Services;
using MicroCMS.Domain.Aggregates.Ai;
using MicroCMS.Domain.Aggregates.Components;
using MicroCMS.Domain.Aggregates.Content;
using MicroCMS.Domain.Aggregates.Identity;
using MicroCMS.Domain.Aggregates.Media;
using MicroCMS.Domain.Aggregates.Pages;
using MicroCMS.Domain.Aggregates.Plugins;
using MicroCMS.Domain.Aggregates.Taxonomy;
using MicroCMS.Domain.Aggregates.Tenant;
using MicroCMS.Domain.Aggregates.Webhooks;
using MicroCMS.Domain.Repositories;
using MicroCMS.Domain.Services;
using MicroCMS.Infrastructure.Ai;
using MicroCMS.Infrastructure.Content;
using MicroCMS.Infrastructure.Identity;
using MicroCMS.Infrastructure.Persistence.Common;
using MicroCMS.Infrastructure.Tenancy;
using MicroCMS.Shared.Ids;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MicroCMS.Infrastructure;

/// <summary>
/// Infrastructure DI registration.
/// Called from the host project (<c>WebHost</c>) <c>Program.cs</c> once on startup.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers all Infrastructure-layer services: EF Core DbContext, repositories,
    /// unit of work, current-user resolver, and date-time provider.
    /// </summary>
    /// <param name="services">The service collection to extend.</param>
    /// <param name="configuration">Application configuration (used to resolve connection strings).</param>
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddHttpContextAccessor();

        RegisterDbContext(services, configuration);
        RegisterRepositories(services);
        RegisterCoreServices(services);
        RegisterTenancyServices(services);

        return services;
    }

    // ── Private registration helpers ──────────────────────────────────────

    private static void RegisterDbContext(
        IServiceCollection services,
        IConfiguration configuration)
    {
        var provider = configuration.GetValue<string>("Database:Provider") ?? "Sqlite";
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? "Data Source=microcms_dev.db";

        services.AddDbContext<ApplicationDbContext>((sp, options) =>
        {
            ConfigureDbProvider(options, provider, connectionString);
            options.EnableSensitiveDataLogging(
                sensitiveDataLoggingEnabled: IsDevEnvironment(configuration));
        });
    }

    private static void ConfigureDbProvider(
        DbContextOptionsBuilder options,
        string provider,
        string connectionString)
    {
        switch (provider.ToUpperInvariant())
        {
            case "POSTGRESQL":
            case "NPGSQL":
                options.UseNpgsql(connectionString, npgsql =>
                    npgsql.MigrationsAssembly("MicroCMS.Infrastructure")
                          .MigrationsHistoryTable("__EFMigrationsHistory", "public"));
                break;

            case "SQLITE":
            default:
                options.UseSqlite(connectionString, sqlite =>
                    sqlite.MigrationsAssembly("MicroCMS.Infrastructure"));
                break;
        }
    }

    private static void RegisterRepositories(IServiceCollection services)
    {
        // Tenant
        services.AddScoped<IRepository<Tenant, TenantId>, EfRepository<Tenant, TenantId>>();
        services.AddScoped<IRepository<TenantSecuritySettings, TenantSecuritySettingsId>, EfRepository<TenantSecuritySettings, TenantSecuritySettingsId>>();

// Sites
  services.AddScoped<IRepository<Site, SiteId>, EfRepository<Site, SiteId>>();
        services.AddScoped<IRepository<SiteSettings, SiteId>, EfRepository<SiteSettings, SiteId>>();
        services.AddScoped<IRepository<ApiClient, ApiClientId>, EfRepository<ApiClient, ApiClientId>>();

        // Content
        services.AddScoped<IRepository<ContentType, ContentTypeId>, EfRepository<ContentType, ContentTypeId>>();
   services.AddScoped<IRepository<Entry, EntryId>, EfRepository<Entry, EntryId>>();
        services.AddScoped<IRepository<Folder, FolderId>, EfRepository<Folder, FolderId>>();

   // Pages
        services.AddScoped<IRepository<Page, PageId>, EfRepository<Page, PageId>>();

      // Media
        services.AddScoped<IRepository<MediaAsset, MediaAssetId>, EfRepository<MediaAsset, MediaAssetId>>();
   services.AddScoped<IRepository<MediaFolder, Guid>, EfRepository<MediaFolder, Guid>>();

        // Taxonomy
      services.AddScoped<IRepository<Category, CategoryId>, EfRepository<Category, CategoryId>>();
        services.AddScoped<IRepository<Tag, TagId>, EfRepository<Tag, TagId>>();

     // Identity
     services.AddScoped<IRepository<User, UserId>, EfRepository<User, UserId>>();
     services.AddScoped<IRepository<RefreshToken, RefreshTokenId>, EfRepository<RefreshToken, RefreshTokenId>>();
     services.AddScoped<IRepository<LoginAttempt, LoginAttemptId>, EfRepository<LoginAttempt, LoginAttemptId>>();

 // Webhooks
      services.AddScoped<IRepository<WebhookSubscription, WebhookSubscriptionId>, EfRepository<WebhookSubscription, WebhookSubscriptionId>>();

     // Content pipeline services
        services.AddScoped<IRepository<Component, ComponentId>, EfRepository<Component, ComponentId>>();

        // AI
      services.AddScoped<IRepository<CopilotConversation, CopilotConversationId>, EfRepository<CopilotConversation, CopilotConversationId>>();
        services.AddScoped<IRepository<AiProviderSettings, AiProviderSettingsId>, EfRepository<AiProviderSettings, AiProviderSettingsId>>();

        // Plugins
        services.AddScoped<IRepository<Plugin, PluginId>, EfRepository<Plugin, PluginId>>();
    }

    private static void RegisterCoreServices(IServiceCollection services)
    {
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<ICurrentUser, HttpContextCurrentUser>();
  services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();
      services.AddScoped<ITokenService, JwtTokenService>();
        services.AddScoped<IPasswordHasher, BCryptPasswordHasher>();
        services.AddScoped<ISecretHasher, Sha256SecretHasher>();
     services.AddScoped<ILlmService, NullLlmService>();
        services.AddScoped<IPreviewSecretProvider, SiteIdPreviewSecretProvider>();
    }

    /// <summary>Sprint 5 — tenancy services: subdomain resolver, resolution middleware, and quota enforcement.</summary>
    private static void RegisterTenancyServices(IServiceCollection services)
    {
        services.AddScoped<ITenantResolver, SubdomainTenantResolver>();
      services.AddScoped<IQuotaService, QuotaService>();
        // TenantResolutionMiddleware is NOT registered here — it is activated by
        // app.UseMiddleware<TenantResolutionMiddleware>() which injects RequestDelegate automatically.
        services.AddScoped<ITenantOnboardingService, TenantOnboardingService>();
    }

    private static bool IsDevEnvironment(IConfiguration configuration) =>
      string.Equals(
  configuration.GetValue<string>("ASPNETCORE_ENVIRONMENT"),
   "Development",
      StringComparison.OrdinalIgnoreCase);
}
