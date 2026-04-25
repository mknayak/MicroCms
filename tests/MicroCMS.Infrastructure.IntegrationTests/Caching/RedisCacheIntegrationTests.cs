using FluentAssertions;
using MicroCMS.Application.Common.Interfaces;
using MicroCMS.Infrastructure.Caching;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using Testcontainers.Redis;
using Xunit;

namespace MicroCMS.Infrastructure.IntegrationTests.Caching;

/// <summary>
/// Sprint 9 — Redis integration tests for <see cref="TwoTierCacheService"/>.
/// Validates L1/L2 round-trips, TTL expiry, <c>RemoveAsync</c>, and tag-based invalidation
/// against a real Redis instance started by Testcontainers.
/// </summary>
[Collection(nameof(RedisCacheCollection))]
public sealed class RedisCacheIntegrationTests(RedisCacheFixture fixture)
{
 // ── Helpers ───────────────────────────────────────────────────────────

    private ICacheService BuildCache(bool withRedis = true)
    {
        var memory = new MemoryCache(new MemoryCacheOptions());
        var options = Options.Create(new CacheOptions
   {
      Provider = withRedis ? "Redis" : "None",
            DefaultTtl = TimeSpan.FromMinutes(5),
            KeyPrefix = "test:"
        });

        IConnectionMultiplexer? redis = withRedis
            ? ConnectionMultiplexer.Connect(fixture.ConnectionString)
         : null;

        return new TwoTierCacheService(memory, options, NullLogger<TwoTierCacheService>.Instance, redis);
    }

    private sealed record SamplePayload(string Name, int Value);

    // ── Tests ─────────────────────────────────────────────────────────────

 [Fact]
    public async Task SetAsync_then_GetAsync_returns_same_value()
    {
        var cache = BuildCache();
        var payload = new SamplePayload("hello", 42);

  await cache.SetAsync("key:1", payload);

        var result = await cache.GetAsync<SamplePayload>("key:1");

        result.Should().NotBeNull();
        result!.Name.Should().Be("hello");
        result.Value.Should().Be(42);
    }

    [Fact]
    public async Task GetAsync_returns_null_for_unknown_key()
    {
        var cache = BuildCache();

        var result = await cache.GetAsync<SamplePayload>("key:missing");

     result.Should().BeNull();
  }

 [Fact]
    public async Task SetAsync_overwrites_existing_value()
    {
        var cache = BuildCache();
        await cache.SetAsync("key:overwrite", new SamplePayload("first", 1));
      await cache.SetAsync("key:overwrite", new SamplePayload("second", 2));

   var result = await cache.GetAsync<SamplePayload>("key:overwrite");

        result!.Name.Should().Be("second");
 result.Value.Should().Be(2);
    }

    [Fact]
    public async Task RemoveAsync_makes_key_unavailable()
    {
        var cache = BuildCache();
   await cache.SetAsync("key:remove", new SamplePayload("bye", 99));

        await cache.RemoveAsync("key:remove");

        var result = await cache.GetAsync<SamplePayload>("key:remove");
  result.Should().BeNull();
    }

    [Fact]
    public async Task SetAsync_respects_explicit_ttl()
    {
        var cache = BuildCache();
    await cache.SetAsync("key:ttl", new SamplePayload("short", 1), expiry: TimeSpan.FromMilliseconds(100));

      // Should still be present immediately
  var immediate = await cache.GetAsync<SamplePayload>("key:ttl");
        immediate.Should().NotBeNull();

        // Wait for TTL to expire
 await Task.Delay(300);

        // L1 (IMemoryCache) and L2 (Redis) should both have expired
 var expired = await cache.GetAsync<SamplePayload>("key:ttl");
        expired.Should().BeNull();
    }

    [Fact]
    public async Task SetWithTagAsync_then_RemoveByTagAsync_evicts_all_tagged_keys()
    {
     var cache = BuildCache();
        const string tag = "test-tag:bulk-evict";

        await cache.SetWithTagAsync("key:tagged:1", new SamplePayload("a", 1), tag);
        await cache.SetWithTagAsync("key:tagged:2", new SamplePayload("b", 2), tag);
        await cache.SetWithTagAsync("key:tagged:3", new SamplePayload("c", 3), tag);

   // Verify all three are present
        (await cache.GetAsync<SamplePayload>("key:tagged:1")).Should().NotBeNull();
   (await cache.GetAsync<SamplePayload>("key:tagged:2")).Should().NotBeNull();
        (await cache.GetAsync<SamplePayload>("key:tagged:3")).Should().NotBeNull();

        await cache.RemoveByTagAsync(tag);

        (await cache.GetAsync<SamplePayload>("key:tagged:1")).Should().BeNull();
        (await cache.GetAsync<SamplePayload>("key:tagged:2")).Should().BeNull();
    (await cache.GetAsync<SamplePayload>("key:tagged:3")).Should().BeNull();
    }

    [Fact]
 public async Task RemoveByTagAsync_for_unknown_tag_does_not_throw()
    {
        var cache = BuildCache();
  var act = async () => await cache.RemoveByTagAsync("tag:does-not-exist");
     await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task Cache_works_without_redis_l2_layer()
    {
    // L1-only mode (no Redis)
        var cache = BuildCache(withRedis: false);
        await cache.SetAsync("key:l1only", new SamplePayload("l1", 7));

        var result = await cache.GetAsync<SamplePayload>("key:l1only");

   result.Should().NotBeNull();
        result!.Name.Should().Be("l1");
    }

    [Fact]
    public async Task L2_Redis_populates_L1_on_read()
    {
        // Build a cache backed by Redis, write to it, then create a *second* instance
        // sharing the same Redis but with a fresh (empty) L1 — simulates a new process node.
        var cache1 = BuildCache();
        await cache1.SetAsync("key:l2-to-l1", new SamplePayload("cross-node", 55));

// Second instance: fresh IMemoryCache so L1 is empty
    var memory2 = new MemoryCache(new MemoryCacheOptions());
        var options2 = Options.Create(new CacheOptions
        {
            Provider = "Redis",
        DefaultTtl = TimeSpan.FromMinutes(5),
         KeyPrefix = "test:"
        });
        var redis2 = ConnectionMultiplexer.Connect(fixture.ConnectionString);
        var cache2 = new TwoTierCacheService(memory2, options2, NullLogger<TwoTierCacheService>.Instance, redis2);

// L1 miss → L2 Redis hit → should be hydrated and returned
        var result = await cache2.GetAsync<SamplePayload>("key:l2-to-l1");
        result.Should().NotBeNull();
        result!.Name.Should().Be("cross-node");
 }
}
