using Knowledge.Analytics.Models;
using Knowledge.Contracts.Types;
using Microsoft.Extensions.Logging;

namespace Knowledge.Analytics.Services;

public interface ICachedAnalyticsService
{
    Task<IEnumerable<ModelUsageStats>> GetModelUsageStatsAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<KnowledgeUsageStats>> GetKnowledgeUsageStatsAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<UsageMetric>> GetUsageHistoryAsync(int days, CancellationToken cancellationToken = default);
    Task<UsageMetric?> GetUsageByConversationAsync(string conversationId, CancellationToken cancellationToken = default);
    Task<IEnumerable<object>> GetUsageTrendsAsync(int days, AiProvider? provider = null, CancellationToken cancellationToken = default);
    Task InvalidateModelStatsAsync(CancellationToken cancellationToken = default);
    Task InvalidateUsageDataAsync(CancellationToken cancellationToken = default);
    Task InvalidateAllAsync(CancellationToken cancellationToken = default);
}

public class CachedAnalyticsService : ICachedAnalyticsService
{
    private readonly IUsageTrackingService _usageTrackingService;
    private readonly IAnalyticsCacheService _cacheService;
    private readonly ILogger<CachedAnalyticsService> _logger;

    // Cache expiration times for different data types
    private static readonly TimeSpan ModelStatsExpiration = TimeSpan.FromMinutes(5); // Model stats change less frequently
    private static readonly TimeSpan KnowledgeStatsExpiration = TimeSpan.FromMinutes(10); // Knowledge stats change infrequently
    private static readonly TimeSpan UsageHistoryExpiration = TimeSpan.FromMinutes(2); // Usage history needs to be fresh
    private static readonly TimeSpan UsageTrendsExpiration = TimeSpan.FromMinutes(5); // Trends can be cached longer
    private static readonly TimeSpan ConversationExpiration = TimeSpan.FromHours(1); // Individual conversations rarely change

    public CachedAnalyticsService(
        IUsageTrackingService usageTrackingService,
        IAnalyticsCacheService cacheService,
        ILogger<CachedAnalyticsService> logger)
    {
        _usageTrackingService = usageTrackingService;
        _cacheService = cacheService;
        _logger = logger;
    }

    public async Task<IEnumerable<ModelUsageStats>> GetModelUsageStatsAsync(CancellationToken cancellationToken = default)
    {
        var cacheKey = AnalyticsCacheService.CacheKeys.ModelStats;
        
        return await _cacheService.GetOrSetAsync(
            cacheKey,
            async () =>
            {
                _logger.LogDebug("Fetching model usage stats from database");
                var stats = await _usageTrackingService.GetModelUsageStatsAsync(cancellationToken);
                return stats.ToList();
            },
            ModelStatsExpiration,
            cancellationToken
        );
    }

    public async Task<IEnumerable<KnowledgeUsageStats>> GetKnowledgeUsageStatsAsync(CancellationToken cancellationToken = default)
    {
        var cacheKey = AnalyticsCacheService.CacheKeys.KnowledgeStats;
        
        return await _cacheService.GetOrSetAsync(
            cacheKey,
            async () =>
            {
                _logger.LogDebug("Fetching knowledge usage stats from database");
                var stats = await _usageTrackingService.GetKnowledgeUsageStatsAsync(cancellationToken);
                return stats.ToList();
            },
            KnowledgeStatsExpiration,
            cancellationToken
        );
    }

    public async Task<IEnumerable<UsageMetric>> GetUsageHistoryAsync(int days, CancellationToken cancellationToken = default)
    {
        var cacheKey = AnalyticsCacheService.CacheKeys.WithParameters(
            AnalyticsCacheService.CacheKeys.UsageTrends, 
            days
        );
        
        return await _cacheService.GetOrSetAsync(
            cacheKey,
            async () =>
            {
                _logger.LogDebug("Fetching usage history for {Days} days from database", days);
                var history = await _usageTrackingService.GetUsageHistoryAsync(days, cancellationToken);
                return history.ToList();
            },
            UsageHistoryExpiration,
            cancellationToken
        );
    }

    public async Task<UsageMetric?> GetUsageByConversationAsync(string conversationId, CancellationToken cancellationToken = default)
    {
        var cacheKey = AnalyticsCacheService.CacheKeys.WithParameters(
            AnalyticsCacheService.CacheKeys.ConversationMetrics,
            conversationId
        );
        
        // Handle nullable return type by using a wrapper approach
        var wrapper = await _cacheService.GetOrSetAsync(
            cacheKey,
            async () =>
            {
                _logger.LogDebug("Fetching usage for conversation {ConversationId} from database", conversationId);
                var result = await _usageTrackingService.GetUsageByConversationAsync(conversationId, cancellationToken);
                return new { Result = result }; // Wrap in non-nullable object
            },
            ConversationExpiration,
            cancellationToken
        );
        
        return wrapper.Result;
    }

    public async Task<IEnumerable<object>> GetUsageTrendsAsync(int days, AiProvider? provider = null, CancellationToken cancellationToken = default)
    {
        var cacheKey = AnalyticsCacheService.CacheKeys.WithParameters(
            AnalyticsCacheService.CacheKeys.UsageTrends,
            days,
            provider?.ToString() ?? "all"
        );
        
        return await _cacheService.GetOrSetAsync(
            cacheKey,
            async () =>
            {
                _logger.LogDebug("Computing usage trends for {Days} days, provider: {Provider}", days, provider);
                
                // Get usage history and transform into trends
                var usageHistory = await _usageTrackingService.GetUsageHistoryAsync(days, cancellationToken);
                
                // Filter by provider if specified
                if (provider.HasValue)
                {
                    usageHistory = usageHistory.Where(u => u.Provider == provider.Value);
                }

                // Group by date and aggregate metrics
                var trends = usageHistory
                    .GroupBy(u => u.Timestamp.Date)
                    .Select(g => new
                    {
                        Date = g.Key,
                        TotalRequests = g.Count(),
                        SuccessfulRequests = g.Count(u => u.WasSuccessful),
                        TotalTokens = g.Sum(u => u.TotalTokens),
                        AverageResponseTime = g.Average(u => u.ResponseTime.TotalMilliseconds),
                        UniqueConversations = g.Select(u => u.ConversationId).Distinct().Count(),
                        ProviderBreakdown = g.GroupBy(u => u.Provider)
                            .ToDictionary(pg => pg.Key.ToString(), pg => pg.Count())
                    })
                    .OrderBy(t => t.Date)
                    .ToList();

                return trends.Cast<object>().ToList();
            },
            UsageTrendsExpiration,
            cancellationToken
        );
    }

    public async Task InvalidateModelStatsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Invalidating model stats cache");
        await _cacheService.InvalidateAsync(AnalyticsCacheService.CacheKeys.ModelStats, cancellationToken);
    }

    public async Task InvalidateUsageDataAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Invalidating usage data cache");
        await _cacheService.InvalidatePatternAsync(AnalyticsCacheService.CacheKeys.UsageTrends, cancellationToken);
        await _cacheService.InvalidatePatternAsync(AnalyticsCacheService.CacheKeys.ConversationMetrics, cancellationToken);
    }

    public async Task InvalidateAllAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Invalidating all analytics cache");
        await _cacheService.InvalidatePatternAsync("analytics:", cancellationToken);
    }
}