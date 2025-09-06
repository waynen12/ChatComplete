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
        services.AddHttpClient();
        
        return services;
    }
}