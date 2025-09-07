using Knowledge.Analytics.Models;
using Microsoft.Extensions.Logging;

namespace Knowledge.Analytics.Services;

public interface ICachedProviderAggregationService
{
    Task<List<ProviderAccountInfo>> GetAllAccountInfoAsync(CancellationToken cancellationToken = default);
    Task<List<ProviderUsageInfo>> GetAllUsageInfoAsync(int days, CancellationToken cancellationToken = default);
    Task<ProviderSummary> GetProviderSummaryAsync(int days, CancellationToken cancellationToken = default);
    Task<List<string>> GetConfiguredProvidersAsync(CancellationToken cancellationToken = default);
    Task<List<string>> GetUnconfiguredProvidersAsync(CancellationToken cancellationToken = default);
    Task RefreshProviderDataAsync(string provider, CancellationToken cancellationToken = default);
    Task RefreshAllProviderDataAsync(CancellationToken cancellationToken = default);
    Task<RateLimitStatus> GetProviderRateLimitStatusAsync(string provider, CancellationToken cancellationToken = default);
}

public class CachedProviderAggregationService : ICachedProviderAggregationService
{
    private readonly ProviderAggregationService _aggregationService;
    private readonly IAnalyticsCacheService _cacheService;
    private readonly IProviderApiRateLimiter _rateLimiter;
    private readonly ILogger<CachedProviderAggregationService> _logger;

    // Cache expiration times for provider data
    private static readonly TimeSpan AccountInfoExpiration = TimeSpan.FromMinutes(15); // Account info changes infrequently
    private static readonly TimeSpan UsageInfoExpiration = TimeSpan.FromMinutes(10); // Usage data needs regular updates
    private static readonly TimeSpan SummaryExpiration = TimeSpan.FromMinutes(5); // Summary data for dashboards
    private static readonly TimeSpan ConfigurationExpiration = TimeSpan.FromHours(1); // Provider configuration rarely changes

    public CachedProviderAggregationService(
        ProviderAggregationService aggregationService,
        IAnalyticsCacheService cacheService,
        IProviderApiRateLimiter rateLimiter,
        ILogger<CachedProviderAggregationService> logger)
    {
        _aggregationService = aggregationService;
        _cacheService = cacheService;
        _rateLimiter = rateLimiter;
        _logger = logger;
    }

    public async Task<List<ProviderAccountInfo>> GetAllAccountInfoAsync(CancellationToken cancellationToken = default)
    {
        var cacheKey = AnalyticsCacheService.CacheKeys.ProviderAccounts;
        
        return await _cacheService.GetOrSetAsync(
            cacheKey,
            async () =>
            {
                _logger.LogDebug("Fetching provider account info with rate limiting");
                
                var accountInfos = new List<ProviderAccountInfo>();
                var providers = new[] { "OpenAi", "Anthropic", "Google", "Ollama" };
                
                foreach (var provider in providers)
                {
                    try
                    {
                        // Check rate limits before making API calls
                        if (!await _rateLimiter.CanMakeRequestAsync(provider, cancellationToken))
                        {
                            _logger.LogWarning("Rate limit exceeded for provider {Provider}, using cached data if available", provider);
                            
                            // Try to get individual cached data for this provider
                            var individualCacheKey = $"{cacheKey}:{provider}";
                            var cachedInfo = await _cacheService.GetAsync<ProviderAccountInfo>(individualCacheKey, cancellationToken);
                            if (cachedInfo != null)
                            {
                                accountInfos.Add(cachedInfo);
                                continue;
                            }
                        }

                        // Make the API call and record the result
                        var startTime = DateTime.UtcNow;
                        ProviderAccountInfo? accountInfo = null;
                        bool successful = false;
                        
                        try
                        {
                            accountInfo = provider switch
                            {
                                "OpenAi" => await GetOpenAiAccountInfoAsync(cancellationToken),
                                "Anthropic" => await GetAnthropicAccountInfoAsync(cancellationToken),
                                "Google" => await GetGoogleAccountInfoAsync(cancellationToken),
                                "Ollama" => await GetOllamaAccountInfoAsync(cancellationToken),
                                _ => null
                            };
                            
                            if (accountInfo != null)
                            {
                                accountInfos.Add(accountInfo);
                                
                                // Cache individual provider data for rate limit scenarios
                                var individualCacheKey = $"{cacheKey}:{provider}";
                                await _cacheService.SetAsync(individualCacheKey, accountInfo, AccountInfoExpiration, cancellationToken);
                                
                                successful = true;
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Failed to get account info for provider {Provider}", provider);
                        }
                        
                        await _rateLimiter.RecordRequestAsync(provider, successful, cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing provider {Provider}", provider);
                    }
                }
                
                return accountInfos;
            },
            AccountInfoExpiration,
            cancellationToken
        );
    }

    public async Task<List<ProviderUsageInfo>> GetAllUsageInfoAsync(int days, CancellationToken cancellationToken = default)
    {
        var cacheKey = AnalyticsCacheService.CacheKeys.WithParameters(
            AnalyticsCacheService.CacheKeys.ProviderUsage,
            days
        );
        
        return await _cacheService.GetOrSetAsync(
            cacheKey,
            async () =>
            {
                _logger.LogDebug("Fetching provider usage info for {Days} days with rate limiting", days);
                return await GetUsageInfoWithRateLimitingAsync(days, cancellationToken);
            },
            UsageInfoExpiration,
            cancellationToken
        );
    }

    public async Task<ProviderSummary> GetProviderSummaryAsync(int days, CancellationToken cancellationToken = default)
    {
        var cacheKey = AnalyticsCacheService.CacheKeys.WithParameters(
            AnalyticsCacheService.CacheKeys.ProviderSummary,
            days
        );
        
        return await _cacheService.GetOrSetAsync(
            cacheKey,
            async () =>
            {
                _logger.LogDebug("Computing provider summary for {Days} days", days);
                
                // Get both account and usage info with rate limiting
                var accountInfoTask = GetAllAccountInfoAsync(cancellationToken);
                var usageInfoTask = GetAllUsageInfoAsync(days, cancellationToken);
                
                await Task.WhenAll(accountInfoTask, usageInfoTask);
                
                var accountInfos = await accountInfoTask;
                var usageInfos = await usageInfoTask;
                
                return new ProviderSummary
                {
                    TotalProviders = accountInfos.Count,
                    ConnectedProviders = accountInfos.Count(a => a.IsConnected),
                    TotalMonthlyCost = usageInfos.Sum(u => u.TotalCost),
                    TotalRequests = usageInfos.Sum(u => u.TotalRequests),
                    AverageSuccessRate = usageInfos.Any() ? usageInfos.Average(u => u.SuccessRate) : 0,
                    LastUpdated = DateTime.UtcNow,
                    ProviderBreakdown = usageInfos.ToDictionary(
                        u => u.Provider,
                        u => new ProviderBreakdown
                        {
                            Cost = u.TotalCost,
                            Requests = u.TotalRequests,
                            SuccessRate = u.SuccessRate,
                            IsConnected = accountInfos.FirstOrDefault(a => a.Provider == u.Provider)?.IsConnected ?? false
                        }
                    )
                };
            },
            SummaryExpiration,
            cancellationToken
        );
    }

    public async Task<List<string>> GetConfiguredProvidersAsync(CancellationToken cancellationToken = default)
    {
        return await _cacheService.GetOrSetAsync(
            "configured_providers",
            () => Task.FromResult(_aggregationService.GetConfiguredProviders()),
            ConfigurationExpiration,
            cancellationToken
        );
    }

    public async Task<List<string>> GetUnconfiguredProvidersAsync(CancellationToken cancellationToken = default)
    {
        return await _cacheService.GetOrSetAsync(
            "unconfigured_providers",
            () => Task.FromResult(_aggregationService.GetUnconfiguredProviders()),
            ConfigurationExpiration,
            cancellationToken
        );
    }

    public async Task RefreshProviderDataAsync(string provider, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Refreshing cached data for provider {Provider}", provider);
        
        // Invalidate cached data for this provider
        await _cacheService.InvalidatePatternAsync($"{AnalyticsCacheService.CacheKeys.ProviderAccounts}:{provider}", cancellationToken);
        await _cacheService.InvalidatePatternAsync($"{AnalyticsCacheService.CacheKeys.ProviderUsage}", cancellationToken);
        await _cacheService.InvalidateAsync(AnalyticsCacheService.CacheKeys.ProviderSummary, cancellationToken);
        
        // Force refresh by calling the methods
        await GetAllAccountInfoAsync(cancellationToken);
    }

    public async Task RefreshAllProviderDataAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Refreshing all cached provider data");
        
        // Invalidate all provider-related cache
        await _cacheService.InvalidatePatternAsync(AnalyticsCacheService.CacheKeys.ProviderAccounts, cancellationToken);
        await _cacheService.InvalidatePatternAsync(AnalyticsCacheService.CacheKeys.ProviderUsage, cancellationToken);
        await _cacheService.InvalidatePatternAsync(AnalyticsCacheService.CacheKeys.ProviderSummary, cancellationToken);
        
        // Force refresh
        await GetAllAccountInfoAsync(cancellationToken);
    }

    public async Task<RateLimitStatus> GetProviderRateLimitStatusAsync(string provider, CancellationToken cancellationToken = default)
    {
        return await _rateLimiter.GetStatusAsync(provider, cancellationToken);
    }

    private async Task<List<ProviderUsageInfo>> GetUsageInfoWithRateLimitingAsync(int days, CancellationToken cancellationToken)
    {
        var usageInfos = new List<ProviderUsageInfo>();
        var providers = new[] { "OpenAi", "Anthropic", "Google", "Ollama" };
        
        foreach (var provider in providers)
        {
            if (await _rateLimiter.CanMakeRequestAsync(provider, cancellationToken))
            {
                try
                {
                    var usageInfo = await GetProviderUsageAsync(provider, days, cancellationToken);
                    if (usageInfo != null)
                    {
                        usageInfos.Add(usageInfo);
                    }
                    await _rateLimiter.RecordRequestAsync(provider, true, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to get usage info for provider {Provider}", provider);
                    await _rateLimiter.RecordRequestAsync(provider, false, cancellationToken);
                }
            }
            else
            {
                _logger.LogWarning("Rate limit exceeded for provider {Provider}, skipping usage info fetch", provider);
            }
        }
        
        return usageInfos;
    }

    // Helper methods to call the original service
    private async Task<ProviderAccountInfo?> GetOpenAiAccountInfoAsync(CancellationToken cancellationToken)
    {
        var accountInfos = await _aggregationService.GetAllAccountInfoAsync(cancellationToken);
        return accountInfos.FirstOrDefault(a => a.Provider == "OpenAi");
    }

    private async Task<ProviderAccountInfo?> GetAnthropicAccountInfoAsync(CancellationToken cancellationToken)
    {
        var accountInfos = await _aggregationService.GetAllAccountInfoAsync(cancellationToken);
        return accountInfos.FirstOrDefault(a => a.Provider == "Anthropic");
    }

    private async Task<ProviderAccountInfo?> GetGoogleAccountInfoAsync(CancellationToken cancellationToken)
    {
        var accountInfos = await _aggregationService.GetAllAccountInfoAsync(cancellationToken);
        return accountInfos.FirstOrDefault(a => a.Provider == "Google");
    }

    private async Task<ProviderAccountInfo?> GetOllamaAccountInfoAsync(CancellationToken cancellationToken)
    {
        var accountInfos = await _aggregationService.GetAllAccountInfoAsync(cancellationToken);
        return accountInfos.FirstOrDefault(a => a.Provider == "Ollama");
    }

    private async Task<ProviderUsageInfo?> GetProviderUsageAsync(string provider, int days, CancellationToken cancellationToken)
    {
        var allUsage = await _aggregationService.GetAllUsageInfoAsync(days, cancellationToken);
        return allUsage.FirstOrDefault(u => u.Provider == provider);
    }
}