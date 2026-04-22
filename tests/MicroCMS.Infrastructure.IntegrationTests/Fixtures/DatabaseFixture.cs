using MicroCMS.Application.Common.Interfaces;
using MicroCMS.Infrastructure;
using MicroCMS.Infrastructure.Persistence.Common;
using MicroCMS.Shared.Ids;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Testcontainers.PostgreSql;
using Xunit;

namespace MicroCMS.Infrastructure.IntegrationTests.Fixtures;

/// <summary>
/// xUnit collection fixture that starts a PostgreSQL container once per test collection,
/// applies migrations, and exposes a factory for creating per-test service scopes.
///
/// Using a single container per collection reduces test run time while still giving
/// each test a fresh scope (and therefore a fresh <see cref="ApplicationDbContext"/>).
/// </summary>
public sealed class DatabaseFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgresContainer = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("microcms_test")
        .WithUsername("microcms")
        .WithPassword("microcms_test_pw")
        .Build();

    public string ConnectionString => _postgresContainer.GetConnectionString();

    public async Task InitializeAsync()
    {
        await _postgresContainer.StartAsync();

        // Apply migrations using a system (no-tenant) scope
        await using var scope = CreateScope(tenantId: null);
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await context.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        await _postgresContainer.DisposeAsync();
    }

    /// <summary>
    /// Creates a fresh async scope for a test, optionally scoped to a specific tenant.
    /// Use <c>await using var scope = fixture.CreateScope()</c> in tests.
    /// Each call returns an independent unit of work.
    /// </summary>
    public AsyncServiceScope CreateScope(TenantId? tenantId = null)
    {
        var provider = BuildServiceProvider(tenantId);
        return provider.CreateAsyncScope();
    }

    // ── Private helpers ───────────────────────────────────────────────────

    private IServiceProvider BuildServiceProvider(TenantId? tenantId)
    {
        var currentUser = BuildCurrentUserMock(tenantId);

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Database:Provider"] = "PostgreSQL",
                ["ConnectionStrings:DefaultConnection"] = _postgresContainer.GetConnectionString()
            })
            .Build();

        var services = new ServiceCollection();

        // Register infrastructure first (which registers HttpContextCurrentUser)
        services.AddInfrastructure(configuration);

        // Override ICurrentUser with the test mock AFTER infrastructure registration.
        // In Microsoft.Extensions.DependencyInjection, GetRequiredService resolves the
        // last registered implementation, so this effectively replaces HttpContextCurrentUser.
        services.AddScoped<ICurrentUser>(_ => currentUser);

        return services.BuildServiceProvider();
    }

    private static ICurrentUser BuildCurrentUserMock(TenantId? tenantId)
    {
        var mock = Substitute.For<ICurrentUser>();
        mock.IsAuthenticated.Returns(tenantId.HasValue);
        mock.TenantId.Returns(tenantId ?? TenantId.Empty);
        mock.UserId.Returns(Guid.NewGuid());
        mock.Email.Returns("test@microcms.dev");
        mock.Roles.Returns(Array.Empty<string>());
        return mock;
    }
}

[CollectionDefinition(nameof(DatabaseCollection))]
public sealed class DatabaseCollection : ICollectionFixture<DatabaseFixture>;
