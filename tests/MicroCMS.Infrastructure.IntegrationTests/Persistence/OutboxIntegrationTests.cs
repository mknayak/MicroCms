using FluentAssertions;
using MicroCMS.Application.Common.Interfaces;
using MicroCMS.Domain.Aggregates.Tenant;
using MicroCMS.Domain.Repositories;
using MicroCMS.Domain.ValueObjects;
using MicroCMS.Infrastructure.IntegrationTests.Fixtures;
using MicroCMS.Infrastructure.Persistence.Common;
using MicroCMS.Shared.Ids;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace MicroCMS.Infrastructure.IntegrationTests.Persistence;

/// <summary>
/// Verifies the transactional outbox pattern: domain events raised on aggregates
/// are automatically written to the <c>OutboxMessages</c> table within the same
/// transaction as the aggregate change.
/// </summary>
[Collection(nameof(DatabaseCollection))]
public sealed class OutboxIntegrationTests
{
    private readonly DatabaseFixture _fixture;

    public OutboxIntegrationTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task SaveChanges_WithNewTenant_WritesOutboxMessageForTenantCreatedEvent()
    {
        // Arrange
        var tenant = Tenant.Create(
            TenantSlug.Create("outbox-test"),
            TenantSettings.Create("Outbox Test Tenant", Locale.English));

        await using var scope = _fixture.CreateScope(tenantId: null);
        var repo = scope.ServiceProvider.GetRequiredService<IRepository<Tenant, TenantId>>();
        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var ctx = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        // Act
        await repo.AddAsync(tenant);
        await uow.SaveChangesAsync();

        // Assert — outbox table should contain the TenantCreatedEvent
        var messages = await ctx.OutboxMessages
            .Where(m => m.ProcessedOnUtc == null)
            .ToListAsync();

        messages.Should().Contain(
            m => m.Type.Contains("TenantCreatedEvent"),
            "a TenantCreatedEvent outbox message must be written when a Tenant is created");
    }

    [Fact]
    public async Task SaveChanges_WithActivatedTenant_WritesOutboxMessageForTenantActivatedEvent()
    {
        // Arrange
        var tenant = Tenant.Create(
            TenantSlug.Create("outbox-activate"),
            TenantSettings.Create("Outbox Activate Test", Locale.English));

        await using var scope = _fixture.CreateScope(tenantId: null);
        var repo = scope.ServiceProvider.GetRequiredService<IRepository<Tenant, TenantId>>();
        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var ctx = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        await repo.AddAsync(tenant);
        await uow.SaveChangesAsync();

        // Act — activate raises TenantActivatedEvent
        tenant.Activate();
        repo.Update(tenant);
        await uow.SaveChangesAsync();

        // Assert
        var activatedMessages = await ctx.OutboxMessages
            .Where(m => m.Type.Contains("TenantActivatedEvent") && m.ProcessedOnUtc == null)
            .ToListAsync();

        activatedMessages.Should().NotBeEmpty(
            "TenantActivatedEvent must be written to the outbox when a tenant is activated");
    }

    [Fact]
    public async Task SaveChanges_ClearsAggregatesDomainEventsAfterWritingToOutbox()
    {
        // Arrange
        var tenant = Tenant.Create(
            TenantSlug.Create("outbox-clear"),
            TenantSettings.Create("Outbox Clear Test", Locale.English));

        await using var scope = _fixture.CreateScope(tenantId: null);
        var repo = scope.ServiceProvider.GetRequiredService<IRepository<Tenant, TenantId>>();
        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        // Act
        await repo.AddAsync(tenant);
        await uow.SaveChangesAsync();

        // Assert — domain events must be cleared after save so they are not re-dispatched
        tenant.DomainEvents.Should().BeEmpty(
            "domain events must be cleared from the aggregate after being written to the outbox");
    }
}
