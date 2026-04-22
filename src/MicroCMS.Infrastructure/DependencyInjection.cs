using MicroCMS.Application.Common.Interfaces;
using MicroCMS.Application.Services;
using MicroCMS.Domain.Aggregates.Content;
using MicroCMS.Domain.Aggregates.Identity;
using MicroCMS.Domain.Aggregates.Media;
using MicroCMS.Domain.Aggregates.Taxonomy;
using MicroCMS.Domain.Aggregates.Tenant;
using MicroCMS.Domain.Repositories;
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
        // Tenant aggregate
        services.AddScoped<IRepository<Tenant, TenantId>, EfRepository<Tenant, TenantId>>();

        // Content aggregates
        services.AddScoped<IRepository<ContentType, ContentTypeId>, EfRepository<ContentType, ContentTypeId>>();
        services.AddScoped<IRepository<Entry, EntryId>, EfRepository<Entry, EntryId>>();

        // Media aggregates
        services.AddScoped<IRepository<MediaAsset, MediaAssetId>, EfRepository<MediaAsset, MediaAssetId>>();

        // Taxonomy aggregates
        services.AddScoped<IRepository<Category, CategoryId>, EfRepository<Category, CategoryId>>();
        services.AddScoped<IRepository<Tag, TagId>, EfRepository<Tag, TagId>>();

        // Media folder
        services.AddScoped<IRepository<MediaFolder, Guid>, EfRepository<MediaFolder, Guid>>();

        // Identity aggregates
        services.AddScoped<IRepository<User, UserId>, EfRepository<User, UserId>>();
    }

    private static void RegisterCoreServices(IServiceCollection services)
    {
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<ICurrentUser, HttpContextCurrentUser>();
        services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();
    }

    /// <summary>Sprint 5 — tenant resolution, quota enforcement, onboarding.</summary>
    private static void RegisterTenancyServices(IServiceCollection services)
    {
        services.AddScoped<ITenantResolver, SubdomainTenantResolver>();
        services.AddScoped<IQuotaService, QuotaService>();
        services.AddScoped<ITenantOnboardingService, TenantOnboardingService>();
    }

    private static bool IsDevEnvironment(IConfiguration configuration) =>
        string.Equals(
            configuration.GetValue<string>("ASPNETCORE_ENVIRONMENT"),
            "Development",
            StringComparison.OrdinalIgnoreCase);
}
