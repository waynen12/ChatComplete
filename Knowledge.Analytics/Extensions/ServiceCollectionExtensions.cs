using Knowledge.Analytics.Services;
using Knowledge.Data.Interfaces;
using Knowledge.Data.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace Knowledge.Analytics.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAnalyticsServices(this IServiceCollection services)
    {
        // Add analytics-specific services (Knowledge.Data repositories already registered by AddSqlitePersistence)
        services.AddScoped<IUsageTrackingService, SqliteUsageTrackingService>();
        
        // Add external provider API services
        services.AddHttpClient<OpenAIProviderApiService>();
        services.AddHttpClient<AnthropicProviderApiService>();
        services.AddHttpClient<GoogleAIProviderApiService>();
        
        // Register provider services
        services.AddScoped<IProviderApiService, OpenAIProviderApiService>();
        services.AddScoped<IProviderApiService, AnthropicProviderApiService>();
        services.AddScoped<IProviderApiService, GoogleAIProviderApiService>();
        
        // Add aggregation service
        services.AddScoped<ProviderAggregationService>();
        
        return services;
    }
}