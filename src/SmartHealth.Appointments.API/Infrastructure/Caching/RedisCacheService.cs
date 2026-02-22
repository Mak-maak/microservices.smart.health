using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace SmartHealth.Appointments.Infrastructure.Caching;

/// <summary>
/// Generic Redis cache service wrapping IDistributedCache.
/// Features:
///   – Get-or-set pattern
///   – Per-key TTL configuration
///   – Cache invalidation helpers
/// </summary>
public sealed class RedisCacheService(
    IDistributedCache cache,
    ILogger<RedisCacheService> logger)
{
    private static readonly TimeSpan DefaultTtl = TimeSpan.FromMinutes(5);

    public async Task<T?> GetAsync<T>(string key, CancellationToken ct = default)
    {
        try
        {
            var bytes = await cache.GetAsync(key, ct);
            return bytes is null ? default : JsonSerializer.Deserialize<T>(bytes);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Cache GET failed for key {Key}.", key);
            return default;
        }
    }

    public async Task SetAsync<T>(string key, T value,
        TimeSpan? ttl = null, CancellationToken ct = default)
    {
        try
        {
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = ttl ?? DefaultTtl
            };
            var bytes = JsonSerializer.SerializeToUtf8Bytes(value);
            await cache.SetAsync(key, bytes, options, ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Cache SET failed for key {Key}.", key);
        }
    }

    public async Task<T?> GetOrSetAsync<T>(string key, Func<Task<T>> factory,
        TimeSpan? ttl = null, CancellationToken ct = default)
    {
        var cached = await GetAsync<T>(key, ct);
        if (cached is not null) return cached;

        var value = await factory();
        if (value is not null)
            await SetAsync(key, value, ttl, ct);

        return value;
    }

    public async Task InvalidateAsync(string key, CancellationToken ct = default)
    {
        try
        {
            await cache.RemoveAsync(key, ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Cache REMOVE failed for key {Key}.", key);
        }
    }
}
