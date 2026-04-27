using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using MicroCMS.Application.Common.Interfaces;
using MicroCMS.Application.Features.ApiClients.Commands;
using MicroCMS.Application.Features.Entries.Queries.Preview;
using MicroCMS.Application.Services;
using MicroCMS.Domain.Aggregates.Ai;
using MicroCMS.Domain.Aggregates.Components;
using MicroCMS.Domain.Aggregates.Content;
using MicroCMS.Domain.Aggregates.Identity;
using MicroCMS.Domain.Aggregates.Locks;
using MicroCMS.Domain.Aggregates.Media;
using MicroCMS.Domain.Aggregates.Pages;
using MicroCMS.Domain.Aggregates.Plugins;
using MicroCMS.Domain.Aggregates.Taxonomy;
using MicroCMS.Domain.Aggregates.Tenant;
using MicroCMS.Domain.Aggregates.Webhooks;
using MicroCMS.Domain.Repositories;
using MicroCMS.Domain.Services;
using MicroCMS.Infrastructure.Ai;
using MicroCMS.Infrastructure.BackgroundJobs;
using MicroCMS.Infrastructure.Caching;
using MicroCMS.Infrastructure.Content;
using MicroCMS.Infrastructure.Identity;
using MicroCMS.Infrastructure.Install;
using MicroCMS.Infrastructure.Persistence.Common;
using MicroCMS.Infrastructure.Search;
using MicroCMS.Infrastructure.Storage.AzureBlob;
using MicroCMS.Infrastructure.Storage.Filesystem;
using MicroCMS.Infrastructure.Storage.Imaging;
using MicroCMS.Infrastructure.Storage.Mime;
using MicroCMS.Infrastructure.Storage.S3;
using MicroCMS.Infrastructure.Storage.Signing;
using MicroCMS.Infrastructure.Storage.VirusScan;
using MicroCMS.Infrastructure.Tenancy;
using MicroCMS.Shared.Ids;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Quartz;
using StackExchange.Redis;

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
        RegisterStorageServices(services, configuration);
        RegisterSearchAndCache(services, configuration);
        RegisterBackgroundJobs(services);

        return services;
    }

    // ── Private registration helpers ──────────────────────────────────────

    private static void RegisterDbContext(
        IServiceCollection services,
        IConfiguration configuration)
    {
        var provider = configuration.GetValue<string>("MicroCMS:Database:Provider") ?? "Sqlite";
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
        services.AddScoped<IRepository<ComponentItem, ComponentItemId>, EfRepository<ComponentItem, ComponentItemId>>();
        services.AddScoped<IRepository<Layout, LayoutId>, EfRepository<Layout, LayoutId>>();
        services.AddScoped<IRepository<PageTemplate, PageTemplateId>, EfRepository<PageTemplate, PageTemplateId>>();

        // AI
        services.AddScoped<IRepository<CopilotConversation, CopilotConversationId>, EfRepository<CopilotConversation, CopilotConversationId>>();
        services.AddScoped<IRepository<AiProviderSettings, AiProviderSettingsId>, EfRepository<AiProviderSettings, AiProviderSettingsId>>();

        // Plugins
        services.AddScoped<IRepository<Plugin, PluginId>, EfRepository<Plugin, PluginId>>();

        // Locks
        services.AddScoped<IRepository<EditLock, EditLockId>, EfRepository<EditLock, EditLockId>>();

        // Site Templates
        services.AddScoped<IRepository<SiteTemplate, SiteTemplateId>, EfRepository<SiteTemplate, SiteTemplateId>>();
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

        // Tenancy / install
        services.AddScoped<ITenantOnboardingService, TenantOnboardingService>();
        services.AddScoped<IInstallationStateService, InstallationStateService>();
    }

    /// <summary>Sprint 5 — tenancy services: subdomain resolver, resolution middleware, and quota enforcement.</summary>
    private static void RegisterTenancyServices(IServiceCollection services)
    {
        services.AddScoped<ITenantResolver, SubdomainTenantResolver>();
        services.AddScoped<IQuotaService, QuotaService>();
        // TenantResolutionMiddleware is NOT registered here — it is activated by UseMiddleware<> in Program.cs
    }

    /// <summary>
    /// Sprint 8 — Media Library: storage providers, MIME inspector, image variant service,
    /// ClamAV scanner, HMAC signing, and the MediaScan background job.
    /// The active storage provider is chosen by <c>Storage:Provider</c> in configuration.
    /// </summary>
    private static void RegisterStorageServices(
        IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSingleton<IMimeTypeInspector, MimeTypeInspector>();
        services.AddSingleton<IImageVariantService, ImageVariantService>();

        services.Configure<HmacSigningOptions>(
     configuration.GetSection(HmacSigningOptions.SectionName));
        services.AddScoped<IStorageSigningService, HmacStorageSigningService>();

        var clamAvEnabled = configuration.GetValue<bool>("ClamAv:Enabled");
        if (clamAvEnabled)
        {
            services.Configure<ClamAvOptions>(configuration.GetSection(ClamAvOptions.SectionName));
            services.AddScoped<IClamAvScanner, ClamAvScanner>();
        }
        else
        {
            services.AddScoped<IClamAvScanner, NoOpClamAvScanner>();
        }

        var storageProvider = configuration.GetValue<string>("MicroCMS:Storage:Provider") ?? "Filesystem";
        RegisterStorageProvider(services, configuration, storageProvider);
    }

    private static void RegisterStorageProvider(
        IServiceCollection services,
        IConfiguration configuration,
        string providerName)
    {
        switch (providerName.ToUpperInvariant())
        {
            case "S3":
                services.Configure<S3StorageOptions>(
                    configuration.GetSection(S3StorageOptions.SectionName));
                services.AddSingleton<IAmazonS3>(sp =>
                {
                    var opts = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<S3StorageOptions>>().Value;
                    var config = new AmazonS3Config { RegionEndpoint = RegionEndpoint.GetBySystemName(opts.Region) };
                    if (!string.IsNullOrEmpty(opts.ServiceUrl))
                    {
                        config.ServiceURL = opts.ServiceUrl;
                        config.ForcePathStyle = true;
                    }
                    return new AmazonS3Client(
                        new BasicAWSCredentials(opts.AccessKeyId, opts.SecretAccessKey),
                        config);
                });
                services.AddScoped<IStorageProvider, S3StorageProvider>();
                break;

            case "AZUREBLOB":
                services.Configure<AzureBlobStorageOptions>(
                    configuration.GetSection(AzureBlobStorageOptions.SectionName));
                services.AddScoped<IStorageProvider, AzureBlobStorageProvider>();
                break;

            case "FILESYSTEM":
            default:
                services.Configure<FilesystemStorageOptions>(
                    configuration.GetSection(FilesystemStorageOptions.SectionName));
                services.AddScoped<IStorageProvider, FilesystemStorageProvider>();
                break;
        }
    }

    /// <summary>Sprint 9 — Search and Cache infrastructure.</summary>
    private static void RegisterSearchAndCache(
      IServiceCollection services,
     IConfiguration configuration)
    {
        // ── Cache ──────────────────────────────────────────────────────────
        services.AddMemoryCache();
        services.Configure<CacheOptions>(configuration.GetSection($"MicroCMS:{CacheOptions.SectionName}"));

        var cacheProvider = configuration.GetValue<string>($"MicroCMS:{CacheOptions.SectionName}:Provider") ?? "None";
        if (string.Equals(cacheProvider, "Redis", StringComparison.OrdinalIgnoreCase))
        {
            var connectionString = configuration.GetValue<string>($"MicroCMS:{CacheOptions.SectionName}:ConnectionString");
            if (!string.IsNullOrWhiteSpace(connectionString))
            {
                services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(connectionString));
            }
        }

        services.AddSingleton<MicroCMS.Application.Common.Interfaces.ICacheService, TwoTierCacheService>();

        // ── Search ─────────────────────────────────────────────────────────
        services.Configure<SearchOptions>(configuration.GetSection($"MicroCMS:{SearchOptions.SectionName}"));

        var searchProvider = configuration.GetValue<string>($"MicroCMS:{SearchOptions.SectionName}:Provider") ?? "Database";
        RegisterSearchProvider(services, configuration, searchProvider);
    }

    private static void RegisterSearchProvider(
       IServiceCollection services,
            IConfiguration configuration,
            string providerName)
    {
        if (string.Equals(providerName, "OpenSearch", StringComparison.OrdinalIgnoreCase))
        {
            services.AddSingleton<OpenSearch.Client.IOpenSearchClient>(sp =>
       {
           var opts = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<SearchOptions>>().Value;
           var pool = new OpenSearch.Net.SingleNodeConnectionPool(new Uri(opts.Endpoint));
           var settings = new OpenSearch.Client.ConnectionSettings(pool);
           if (!string.IsNullOrEmpty(opts.Username))
           {
               settings = settings.BasicAuthentication(opts.Username, opts.Password);
           }
           return new OpenSearch.Client.OpenSearchClient(settings);
       });
            services.AddSingleton<MicroCMS.Application.Common.Interfaces.ISearchService, OpenSearchService>();
        }
        else if (string.Equals(providerName, "None", StringComparison.OrdinalIgnoreCase))
        {
            services.AddSingleton<MicroCMS.Application.Common.Interfaces.ISearchService, NullSearchService>();
        }
        else
        {
            // Default: "Database" — SQL LIKE via EF Core; no external dependencies required.
            services.AddScoped<MicroCMS.Application.Common.Interfaces.ISearchService, DatabaseSearchService>();
        }
    }

    /// <summary>Registers the MediaScan Quartz job to run every 30 seconds.</summary>
    private static void RegisterBackgroundJobs(IServiceCollection services)
    {
        services.AddQuartz(q =>
        {
            var jobKey = new JobKey("MediaScanJob");
            q.AddJob<MediaScanJob>(opts => opts.WithIdentity(jobKey));
            q.AddTrigger(opts => opts
                .ForJob(jobKey)
                .WithIdentity("MediaScanTrigger")
                .WithSimpleSchedule(s => s
                    .WithIntervalInSeconds(30)
                    .RepeatForever()));
        });

        services.AddQuartzHostedService(opts => opts.WaitForJobsToComplete = true);
    }

    private static bool IsDevEnvironment(IConfiguration configuration) =>
        string.Equals(
            configuration.GetValue<string>("ASPNETCORE_ENVIRONMENT"),
            "Development",
            StringComparison.OrdinalIgnoreCase);
}
