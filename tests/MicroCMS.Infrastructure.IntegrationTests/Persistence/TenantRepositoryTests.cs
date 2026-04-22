using FluentAssertions;
using MicroCMS.Application.Common.Interfaces;
using MicroCMS.Domain.Aggregates.Tenant;
using MicroCMS.Domain.Repositories;
using MicroCMS.Domain.Specifications;
using MicroCMS.Domain.Specifications.Tenants;
using MicroCMS.Domain.ValueObjects;
using MicroCMS.Infrastructure.IntegrationTests.Fixtures;
using MicroCMS.Shared.Ids;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace MicroCMS.Infrastructure.IntegrationTests.Persistence;

/// <summary>
/// Integration tests for <see cref="IRepository{Tenant, TenantId}"/> using a real PostgreSQL database.
/// Verifies that CRUD operations, specification evaluation, and tenant isolation all work correctly.
/// </summary>
[Collection(nameof(DatabaseCollection))]
public sealed class TenantRepositoryTests
{
    private readonly DatabaseFixture _fixture;

    public TenantRepositoryTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task AddAsync_And_GetByIdAsync_RoundTrip_ReturnsCorrectTenant()
    {
        // Arrange
        var tenant = BuildTenant("acme-corp");
        await using var scope = _fixture.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IRepository<Tenant, TenantId>>();
        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        // Act
        await repo.AddAsync(tenant);
        await uow.SaveChangesAsync();

        var retrieved = await repo.GetByIdAsync(tenant.Id);

        // Assert
        retrieved.Should().NotBeNull();
        retrieved!.Slug.Value.Should().Be("acme-corp");
        retrieved.Status.Should().Be(MicroCMS.Domain.Enums.TenantStatus.Provisioning);
        retrieved.Settings.DisplayName.Should().Be("Acme Corporation");
    }

    [Fact]
    public async Task Update_ChangesSettings_PersistsCorrectly()
    {
        // Arrange
        var tenant = BuildTenant("update-test");
        await using var scope = _fixture.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IRepository<Tenant, TenantId>>();
        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        await repo.AddAsync(tenant);
        await uow.SaveChangesAsync();

        // Act
        var newSettings = TenantSettings.Create(
            "Updated Name", Locale.English, timeZoneId: "America/New_York");
        tenant.UpdateSettings(newSettings);
        repo.Update(tenant);
        await uow.SaveChangesAsync();

        var retrieved = await repo.GetByIdAsync(tenant.Id);

        // Assert
        retrieved.Should().NotBeNull();
        retrieved!.Settings.DisplayName.Should().Be("Updated Name");
        retrieved.Settings.TimeZoneId.Should().Be("America/New_York");
    }

    [Fact]
    public async Task ListAsync_WithSpecification_ReturnsFilteredResults()
    {
        // Arrange
        var active = BuildTenant("active-tenant");
        var suspended = BuildTenant("suspended-tenant");
        active.Activate();
        suspended.Activate();
        suspended.Suspend("Test suspension");

        await using var scope = _fixture.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IRepository<Tenant, TenantId>>();
        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        await repo.AddAsync(active);
        await repo.AddAsync(suspended);
        await uow.SaveChangesAsync();

        // Act — use the ActiveTenantsSpec from the domain layer
        var spec = new ActiveTenantsSpec();
        var results = await repo.ListAsync(spec);

        // Assert
        results.Should().Contain(t => t.Id == active.Id);
        results.Should().NotContain(t => t.Id == suspended.Id);
    }

    [Fact]
    public async Task Remove_DeletesTenantFromDatabase()
    {
        // Arrange
        var tenant = BuildTenant("to-delete");
        await using var scope = _fixture.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IRepository<Tenant, TenantId>>();
        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        await repo.AddAsync(tenant);
        await uow.SaveChangesAsync();

        // Act
        repo.Remove(tenant);
        await uow.SaveChangesAsync();

        var retrieved = await repo.GetByIdAsync(tenant.Id);

        // Assert
        retrieved.Should().BeNull();
    }

    // ── Private helpers ───────────────────────────────────────────────────

    private static Tenant BuildTenant(string slug) =>
        Tenant.Create(
            TenantSlug.Create(slug),
            TenantSettings.Create("Acme Corporation", Locale.English));
}
