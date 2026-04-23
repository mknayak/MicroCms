using System.Collections.Concurrent;
using System.Text.Json;
using MicroCMS.Application.Common.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace MicroCMS.Infrastructure.Caching;

/// <summary>
/// Sprint 9 — two-tier cache implementation.
/// L1: in-process <see cref="IMemoryCache"/> for sub-millisecond reads.
/// L2: optional Redis for cross-instance sharing when configured.
///
/// Tag invalidation is supported via an in-memory tag → key index that is consulted by
/// <see cref="RemoveByTagAsync"/>; this covers the common "invalidate all entries for a tenant"
/// pattern from §18.4 of the design. A restart loses the tag index — that is acceptable because
/// cache entries are rebuilt on read.
/// </summary>
internal sealed class TwoTierCacheService : ICacheService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IMemoryCache _memory;
    private readonly IConnectionMultiplexer? _redis;
    private readonly CacheOptions _options;
    private readonly ILogger<TwoTierCacheService> _logger;

    // tag -> set of keys. ConcurrentDictionary gives thread-safe add/remove.
    private static readonly ConcurrentDictionary<string, ConcurrentDictionary<string, byte>> _tagIndex = new();

    public TwoTierCacheService(
        IMemoryCache memory,
        IOptions<CacheOptions> options,
        ILogger<TwoTierCacheService> logger,
      IConnectionMultiplexer? redis = null)
    {
    _memory = memory;
  _options = options.Value;
      _logger = logger;
      _redis = redis;
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
      where T : class
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        if (_memory.TryGetValue(key, out T? l1Hit) && l1Hit is not null)
      return l1Hit;

   if (_redis is null) return null;

        try
  {
 var db = _redis.GetDatabase();
    var value = await db.StringGetAsync(PrefixedKey(key));
     if (value.IsNullOrEmpty) return null;

   var deserialised = JsonSerializer.Deserialize<T>(value!, JsonOptions);
            if (deserialised is not null)
      {
   _memory.Set(key, deserialised, _options.DefaultTtl);
    }
          return deserialised;
        }
        catch (Exception ex)
     {
       _logger.LogWarning(ex, "Redis L2 read failed for key {Key}; returning null.", key);
   return null;
        }
    }

    public async Task SetAsync<T>(
   string key,
        T value,
        TimeSpan? expiry = null,
        CancellationToken cancellationToken = default)
        where T : class
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(value);

        var ttl = expiry ?? _options.DefaultTtl;
        _memory.Set(key, value, ttl);

        if (_redis is null) return;

     try
        {
         var db = _redis.GetDatabase();
            var json = JsonSerializer.Serialize(value, JsonOptions);
        await db.StringSetAsync(PrefixedKey(key), json, ttl);
        }
        catch (Exception ex)
    {
            _logger.LogWarning(ex, "Redis L2 write failed for key {Key}.", key);
        }
 }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
 {
    ArgumentException.ThrowIfNullOrWhiteSpace(key);
        _memory.Remove(key);

      if (_redis is not null)
 {
            try
  {
      await _redis.GetDatabase().KeyDeleteAsync(PrefixedKey(key));
      }
            catch (Exception ex)
            {
          _logger.LogWarning(ex, "Redis L2 delete failed for key {Key}.", key);
         }
  }
    }

    public async Task RemoveByTagAsync(string tag, CancellationToken cancellationToken = default)
    {
      ArgumentException.ThrowIfNullOrWhiteSpace(tag);
        if (!_tagIndex.TryRemove(tag, out var keys)) return;

        foreach (var key in keys.Keys)
        {
  await RemoveAsync(key, cancellationToken);
}
    }

    /// <summary>Associates <paramref name="key"/> with <paramref name="tag"/> for bulk invalidation.</summary>
    internal static void AssociateTag(string key, string tag)
    {
        var set = _tagIndex.GetOrAdd(tag, _ => new ConcurrentDictionary<string, byte>());
        set.TryAdd(key, 0);
    }

  private string PrefixedKey(string key) => _options.KeyPrefix + key;
}
