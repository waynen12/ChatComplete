using Knowledge.Analytics.Services;
using Knowledge.Data.Interfaces;
using Knowledge.Data.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Security.Authentication;

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
        
        // Add Ollama-specific analytics service
        services.AddScoped<IOllamaAnalyticsService, OllamaAnalyticsService>();
        
        // Add external provider API services with SSL/TLS configuration
        services.AddHttpClient<OpenAIProviderApiService>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
            client.DefaultRequestHeaders.Add("User-Agent", "AIKnowledgeManager/1.0");
        })
        .ConfigurePrimaryHttpMessageHandler((serviceProvider) => 
        {
            var config = serviceProvider.GetRequiredService<IConfiguration>();
            var handler = new HttpClientHandler();
            
            // Enable TLS 1.2 and 1.3 support
            handler.SslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13;
            
            // Enable automatic decompression for better performance
            handler.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
            
            // Check if SSL validation bypass is enabled in configuration (for development)
            var bypassSslValidation = config.GetValue<bool>("Development:BypassSslValidation", false);
            if (bypassSslValidation)
            {
                handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true;
            }
            
            return handler;
        });
        
        services.AddHttpClient<AnthropicProviderApiService>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
        });
        
        services.AddHttpClient<GoogleAIProviderApiService>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
        });
        
        // Register provider services
        services.AddScoped<IProviderApiService, OpenAIProviderApiService>();
        services.AddScoped<IProviderApiService, AnthropicProviderApiService>();
        services.AddScoped<IProviderApiService, GoogleAIProviderApiService>();
        
        // Register Ollama provider service with HttpClient
        services.AddHttpClient<OllamaProviderApiService>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(5); // Quick timeout for health checks
        });
        services.AddScoped<IProviderApiService, OllamaProviderApiService>();
        
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