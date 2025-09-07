using Knowledge.Analytics.Services;
using Knowledge.Data.Interfaces;
using Knowledge.Data.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Knowledge.Analytics.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAnalyticsServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Add caching services (must be registered before cached services)
        services.AddMemoryCache();
        services.AddSingleton<IAnalyticsCacheService, AnalyticsCacheService>();
        services.AddSingleton<IProviderApiRateLimiter, ProviderApiRateLimiter>();
        
        // Add analytics-specific services (Knowledge.Data repositories already registered by AddSqlitePersistence)
        services.AddScoped<IUsageTrackingService, SqliteUsageTrackingService>();
        
        // Add cached analytics services
        services.AddScoped<ICachedAnalyticsService, CachedAnalyticsService>();
        
        // Add external provider API services
        services.AddHttpClient<OpenAIProviderApiService>();
        services.AddHttpClient<AnthropicProviderApiService>();
        services.AddHttpClient<GoogleAIProviderApiService>();
        
        // Register provider services
        services.AddScoped<IProviderApiService, OpenAIProviderApiService>();
        services.AddScoped<IProviderApiService, AnthropicProviderApiService>();
        services.AddScoped<IProviderApiService, GoogleAIProviderApiService>();
        
        // Add aggregation services (both regular and cached)
        services.AddScoped<ProviderAggregationService>();
        services.AddScoped<ICachedProviderAggregationService, CachedProviderAggregationService>();
        
        // Add background sync services
        services.Configure<BackgroundSyncOptions>(configuration.GetSection("Analytics:BackgroundSync"));
        services.AddSingleton<IBackgroundSyncService, BackgroundSyncService>();
        services.AddHostedService<BackgroundSyncService>();
        
        return services;
    }
}