using MicroCMS.Application.Common.Interfaces;
using MicroCMS.Infrastructure.Persistence.Common;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace MicroCMS.E2E.Tests.Fixtures;

/// <summary>
/// In-process WebApplicationFactory that:
/// - Replaces the production database with a per-run temp-file SQLite database.
/// - Disables external Redis and OpenSearch (falls back to InMemory / Null implementations).
/// - Primes the installation-state service so the guard middleware lets requests through.
/// - Exposes a <see cref="CreateAuthenticatedClient"/> helper that attaches a valid JWT.
///
/// A file-based SQLite database (rather than in-memory) is used so the schema
/// persists across the multiple <see cref="DbContext"/> connection lifetimes created
/// during a single test run.
/// </summary>
public sealed class MicroCmsWebApplicationFactory : WebApplicationFactory<Program>, IDisposable
{
    private readonly string _testDbPath =
        Path.Combine(Path.GetTempPath(), $"microcms_e2e_{Guid.NewGuid():N}.db");

    private string TestConnectionString => $"Data Source={_testDbPath}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            // Override configuration for a self-contained, isolated test run.
            // NOTE: TrustedClients / JWT settings are intentionally NOT overridden here.
            // AddSecurityServices() reads those values before WebApplicationFactory's
            // ConfigureAppConfiguration runs (services are registered before Build()),
            // so JWT validation keys must match appsettings.json.
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = TestConnectionString,
                ["MicroCMS:Database:Provider"] = "Sqlite",
                ["MicroCMS:Cache:Provider"] = "InMemory",
                ["MicroCMS:Search:Provider"] = "None",
                ["MicroCMS:Storage:Provider"] = "Database",
                ["Telemetry:Enabled"] = "false",
            });
        });

        builder.ConfigureServices(services =>
        {
            // Remove all DbContext-related registrations so we can wire up a fresh SQLite one
            var dbContextDescriptors = services
                .Where(d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>)
                         || d.ServiceType == typeof(ApplicationDbContext)
                         || (d.ServiceType.IsGenericType &&
                             d.ServiceType.GetGenericTypeDefinition() == typeof(DbContextOptions<>) &&
                             d.ServiceType.GenericTypeArguments[0] == typeof(ApplicationDbContext)))
                .ToList();

            foreach (var d in dbContextDescriptors)
                services.Remove(d);

            // Register a file-based SQLite DbContext for the test run
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlite(TestConnectionString));
        });
    }

    /// <summary>
    /// Ensures the SQLite test database schema is created.
    /// Must be called once before the first request is sent.
    /// </summary>
    public async Task InitializeAsync()
    {
        // Build the schema using the app's fully-configured service provider
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await db.Database.EnsureCreatedAsync();

        // Prime installation state so the guard middleware passes all requests
        var installState = scope.ServiceProvider.GetRequiredService<IInstallationStateService>();
        installState.MarkInstalled();
    }

    /// <summary>
    /// Creates an <see cref="HttpClient"/> that sends requests with a pre-minted JWT for
    /// the given role, valid against the <c>TrustedClients:Admin</c> settings in appsettings.json.
    /// </summary>
    public HttpClient CreateAuthenticatedClient(
        string tenantId = "00000000-0000-0000-0000-000000000001",
        string role = "TenantAdmin")
    {
        var client = CreateClient();
        // Secret/Issuer/Audience must match appsettings.json TrustedClients:Admin
        var token = JwtTestHelper.GenerateToken(
            secret: "5DE7C44A99A04FF4A9241CE81BA3DDF9",
            issuer: "microcms-admin",
            audience: "microcms-api",
            tenantId: tenantId,
            role: role);

        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        return client;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        // Clean up the temp database file after the test run
        if (disposing && File.Exists(_testDbPath))
            File.Delete(_testDbPath);
    }
}
