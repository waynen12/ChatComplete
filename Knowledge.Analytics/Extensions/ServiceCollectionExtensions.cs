using Knowledge.Analytics.Services;
using Knowledge.Data.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace Knowledge.Analytics.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAnalyticsServices(this IServiceCollection services, string databasePath)
    {
        // Add the Knowledge.Data layer
        services.AddKnowledgeData(databasePath);
        
        // Add analytics-specific services
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