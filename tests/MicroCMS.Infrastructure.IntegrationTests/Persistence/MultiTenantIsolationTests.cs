using FluentAssertions;
using MicroCMS.Domain.Aggregates.Content;
using MicroCMS.Domain.Aggregates.Taxonomy;
using MicroCMS.Domain.ValueObjects;
using MicroCMS.Infrastructure.IntegrationTests.Fixtures;
using MicroCMS.Infrastructure.Persistence.Common;
using MicroCMS.Shared.Ids;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace MicroCMS.Infrastructure.IntegrationTests.Persistence;

/// <summary>
/// Security tests: verifies that the global query filter enforces tenant isolation.
/// A session authenticated as Tenant A must not be able to read Tenant B's data,
/// even when querying the same table.
///
/// Each test uses unique GUIDs in slug suffixes to avoid unique-constraint collisions
/// when the shared database retains data from prior test runs.
/// </summary>
[Collection(nameof(DatabaseCollection))]
public sealed class MultiTenantIsolationTests
{
    private readonly DatabaseFixture _fixture;

    public MultiTenantIsolationTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Entry_QueryAsTenantA_DoesNotReturnTenantBEntries()
    {
        // Use a unique suffix to avoid slug collisions across test runs
        var suffix = Guid.NewGuid().ToString("N")[..8];

        var tenantAId = TenantId.New();
        var tenantBId = TenantId.New();
        var siteId = SiteId.New();
        var contentTypeId = ContentTypeId.New();
        var authorId = Guid.NewGuid();

        var entryA = Entry.Create(
            tenantAId, siteId, contentTypeId,
            Slug.Create($"article-a-{suffix}"), Locale.English, authorId);

        var entryB = Entry.Create(
            tenantBId, siteId, contentTypeId,
            Slug.Create($"article-b-{suffix}"), Locale.English, authorId);

        // Seed using a system scope (bypasses query filter — no authenticated tenant)
        await using (var systemScope = _fixture.CreateScope(tenantId: null))
        {
            var seedCtx = systemScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            seedCtx.Entries.Add(entryA);
            seedCtx.Entries.Add(entryB);
            await seedCtx.SaveChangesAsync();
        }

        // Act — query using Tenant A's authenticated session
        await using var tenantAScope = _fixture.CreateScope(tenantAId);
        var readCtx = tenantAScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var results = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions
            .ToListAsync(readCtx.Entries);

        // Assert
        results.Should().Contain(e => e.Id == entryA.Id,
            "Tenant A must be able to read its own entries");
        results.Should().NotContain(e => e.Id == entryB.Id,
            "Tenant A must never see Tenant B's entries (global query filter violation)");
    }

    [Fact]
    public async Task Tag_QueryAsTenantA_DoesNotReturnTenantBTags()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var tenantAId = TenantId.New();
        var tenantBId = TenantId.New();
        var siteId = SiteId.New();

        var tagA = Tag.Create(tenantAId, siteId, $"TagForA-{suffix}", Slug.Create($"tag-for-a-{suffix}"));
        var tagB = Tag.Create(tenantBId, siteId, $"TagForB-{suffix}", Slug.Create($"tag-for-b-{suffix}"));

        await using (var systemScope = _fixture.CreateScope(tenantId: null))
        {
            var seedCtx = systemScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            seedCtx.Tags.Add(tagA);
            seedCtx.Tags.Add(tagB);
            await seedCtx.SaveChangesAsync();
        }

        // Act — authenticate as Tenant A
        await using var tenantAScope = _fixture.CreateScope(tenantAId);
        var readCtx = tenantAScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var allVisible = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions
            .ToListAsync(readCtx.Tags);

        // Assert
        allVisible.Should().Contain(t => t.Id == tagA.Id,
            "Tenant A can see its own tags");
        allVisible.Should().NotContain(t => t.Id == tagB.Id,
            "global HasQueryFilter must prevent cross-tenant data leakage");
    }
}
