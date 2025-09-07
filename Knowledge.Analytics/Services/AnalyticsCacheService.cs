using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Text.Json;

namespace Knowledge.Analytics.Services;

public interface IAnalyticsCacheService
{
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class;
    Task SetAsync<T>(string key, T value, TimeSpan expiration, CancellationToken cancellationToken = default) where T : class;
    Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan expiration, CancellationToken cancellationToken = default) where T : class;
    Task InvalidateAsync(string key, CancellationToken cancellationToken = default);
    Task InvalidatePatternAsync(string pattern, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default);
    Task<CacheStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default);
}

public record CacheStatistics
{
    public int TotalEntries { get; init; }
    public int HitCount { get; init; }
    public int MissCount { get; init; }
    public double HitRate => TotalRequests > 0 ? (double)HitCount / TotalRequests * 100 : 0;
    public int TotalRequests => HitCount + MissCount;
    public TimeSpan AverageResponseTime { get; init; }
    public Dictionary<string, int> KeyPrefixStats { get; init; } = new();
}

public class AnalyticsCacheService : IAnalyticsCacheService
{
    private readonly IMemoryCache _memoryCache;
    private readonly ILogger<AnalyticsCacheService> _logger;
    private readonly ConcurrentDictionary<string, DateTime> _keyTimestamps = new();
    private readonly ConcurrentDictionary<string, int> _accessCounts = new();
    private int _hitCount = 0;
    private int _missCount = 0;
    private readonly List<TimeSpan> _responseTimes = new();
    private readonly object _statsLock = new();

    // Cache key prefixes for different data types
    public static class CacheKeys
    {
        public const string ModelStats = "analytics:models:stats";
        public const string KnowledgeStats = "analytics:knowledge:stats";
        public const string UsageTrends = "analytics:usage:trends";
        public const string CostBreakdown = "analytics:cost:breakdown";
        public const string ProviderAccounts = "analytics:providers:accounts";
        public const string ProviderUsage = "analytics:providers:usage";
        public const string ProviderSummary = "analytics:providers:summary";
        public const string ConversationMetrics = "analytics:conversations:metrics";

        public static string WithParameters(string baseKey, params object[] parameters) =>
            $"{baseKey}:{string.Join(":", parameters.Select(p => p?.ToString() ?? "null"))}";
    }

    public AnalyticsCacheService(IMemoryCache memoryCache, ILogger<AnalyticsCacheService> logger)
    {
        _memoryCache = memoryCache;
        _logger = logger;
    }

    public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class
    {
        var startTime = DateTime.UtcNow;
        
        try
        {
            if (_memoryCache.TryGetValue(key, out var cachedValue) && cachedValue is T typedValue)
            {
                RecordHit(key, DateTime.UtcNow - startTime);
                _logger.LogDebug("Cache hit for key: {Key}", key);
                return Task.FromResult<T?>(typedValue);
            }

            RecordMiss(key, DateTime.UtcNow - startTime);
            _logger.LogDebug("Cache miss for key: {Key}", key);
            return Task.FromResult<T?>(null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving from cache for key: {Key}", key);
            RecordMiss(key, DateTime.UtcNow - startTime);
            return Task.FromResult<T?>(null);
        }
    }

    public Task SetAsync<T>(string key, T value, TimeSpan expiration, CancellationToken cancellationToken = default) where T : class
    {
        try
        {
            var options = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiration,
                Priority = CacheItemPriority.Normal,
                Size = EstimateSize(value)
            };

            // Add cache eviction callback for logging
            options.RegisterPostEvictionCallback((k, v, reason, state) =>
            {
                _keyTimestamps.TryRemove(k.ToString() ?? string.Empty, out _);
                _accessCounts.TryRemove(k.ToString() ?? string.Empty, out _);
                _logger.LogDebug("Cache entry evicted: {Key}, Reason: {Reason}", k, reason);
            });

            _memoryCache.Set(key, value, options);
            _keyTimestamps[key] = DateTime.UtcNow;
            
            _logger.LogDebug("Cache set for key: {Key}, Expiration: {Expiration}", key, expiration);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting cache for key: {Key}", key);
        }

        return Task.CompletedTask;
    }

    public async Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan expiration, CancellationToken cancellationToken = default) where T : class
    {
        var cached = await GetAsync<T>(key, cancellationToken);
        if (cached != null)
        {
            return cached;
        }

        try
        {
            _logger.LogDebug("Cache miss, executing factory for key: {Key}", key);
            var value = await factory();
            await SetAsync(key, value, expiration, cancellationToken);
            return value;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing factory for key: {Key}", key);
            throw;
        }
    }

    public Task InvalidateAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            _memoryCache.Remove(key);
            _keyTimestamps.TryRemove(key, out _);
            _accessCounts.TryRemove(key, out _);
            
            _logger.LogDebug("Cache invalidated for key: {Key}", key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invalidating cache for key: {Key}", key);
        }

        return Task.CompletedTask;
    }

    public Task InvalidatePatternAsync(string pattern, CancellationToken cancellationToken = default)
    {
        try
        {
            var keysToRemove = _keyTimestamps.Keys
                .Where(k => k.StartsWith(pattern, StringComparison.OrdinalIgnoreCase))
                .ToList();

            foreach (var key in keysToRemove)
            {
                _memoryCache.Remove(key);
                _keyTimestamps.TryRemove(key, out _);
                _accessCounts.TryRemove(key, out _);
            }

            _logger.LogDebug("Cache invalidated for pattern: {Pattern}, Keys removed: {Count}", pattern, keysToRemove.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invalidating cache for pattern: {Pattern}", pattern);
        }

        return Task.CompletedTask;
    }

    public Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_keyTimestamps.ContainsKey(key));
    }

    public Task<CacheStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default)
    {
        lock (_statsLock)
        {
            var keyPrefixStats = _keyTimestamps.Keys
                .GroupBy(k => k.Split(':').FirstOrDefault() ?? "unknown")
                .ToDictionary(g => g.Key, g => g.Count());

            var avgResponseTime = _responseTimes.Count > 0 
                ? TimeSpan.FromMilliseconds(_responseTimes.Average(t => t.TotalMilliseconds))
                : TimeSpan.Zero;

            return Task.FromResult(new CacheStatistics
            {
                TotalEntries = _keyTimestamps.Count,
                HitCount = _hitCount,
                MissCount = _missCount,
                AverageResponseTime = avgResponseTime,
                KeyPrefixStats = keyPrefixStats
            });
        }
    }

    private void RecordHit(string key, TimeSpan responseTime)
    {
        lock (_statsLock)
        {
            _hitCount++;
            _responseTimes.Add(responseTime);
            _accessCounts.AddOrUpdate(key, 1, (k, v) => v + 1);
            
            // Keep response times list bounded
            if (_responseTimes.Count > 1000)
            {
                _responseTimes.RemoveRange(0, 500);
            }
        }
    }

    private void RecordMiss(string key, TimeSpan responseTime)
    {
        lock (_statsLock)
        {
            _missCount++;
            _responseTimes.Add(responseTime);
            
            if (_responseTimes.Count > 1000)
            {
                _responseTimes.RemoveRange(0, 500);
            }
        }
    }

    private static int EstimateSize<T>(T value) where T : class
    {
        try
        {
            // Simple size estimation based on JSON serialization
            var json = JsonSerializer.Serialize(value);
            return json.Length * 2; // Rough estimate for UTF-16 encoding
        }
        catch
        {
            // Fallback to a reasonable default
            return 1024; // 1KB default
        }
    }
}