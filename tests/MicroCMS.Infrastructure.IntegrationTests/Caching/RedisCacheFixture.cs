using Testcontainers.Redis;
using Xunit;

namespace MicroCMS.Infrastructure.IntegrationTests.Caching;

/// <summary>
/// xUnit collection fixture that starts a single Redis container shared across all
/// <see cref="RedisCacheIntegrationTests"/>. The container is started once and disposed
/// at the end of the test run to keep total execution time low.
/// </summary>
public sealed class RedisCacheFixture : IAsyncLifetime
{
    private readonly RedisContainer _redisContainer = new RedisBuilder()
 .WithImage("redis:7-alpine")
   .Build();

    public string ConnectionString => _redisContainer.GetConnectionString();

    public Task InitializeAsync() => _redisContainer.StartAsync();

    public Task DisposeAsync() => _redisContainer.DisposeAsync().AsTask();
}

[CollectionDefinition(nameof(RedisCacheCollection))]
public sealed class RedisCacheCollection : ICollectionFixture<RedisCacheFixture>;
