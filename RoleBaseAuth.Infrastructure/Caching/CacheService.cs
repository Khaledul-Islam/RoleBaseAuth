using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace RoleBaseAuth.Infrastructure.Caching;

public interface ICacheService
{
    Task<T> GetAsync<T>(string key);
    Task SetAsync<T>(string key, T value, TimeSpan? expiration = null);
    Task RemoveAsync(string key);
    Task<bool> ExistsAsync(string key);
}

public class RedisCacheService(IDistributedCache cache, ILogger<RedisCacheService> logger) : ICacheService
{
    public async Task<T> GetAsync<T>(string key)
    {
        try
        {
            var value = await cache.GetStringAsync(key);
            return value == null ? default : JsonSerializer.Deserialize<T>(value);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting cache key: {Key}", key);
            return default;
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null)
    {
        try
        {
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiration ?? TimeSpan.FromMinutes(10)
            };

            var serialized = JsonSerializer.Serialize(value);
            await cache.SetStringAsync(key, serialized, options);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error setting cache key: {Key}", key);
        }
    }

    public async Task RemoveAsync(string key)
    {
        try
        {
            await cache.RemoveAsync(key);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error removing cache key: {Key}", key);
        }
    }

    public async Task<bool> ExistsAsync(string key)
    {
        try
        {
            var value = await cache.GetStringAsync(key);
            return value != null;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error checking cache key existence: {Key}", key);
            return false;
        }
    }
}
