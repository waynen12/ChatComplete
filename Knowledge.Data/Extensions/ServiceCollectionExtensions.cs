using Knowledge.Data.Interfaces;
using Knowledge.Data.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace Knowledge.Data.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddKnowledgeData(this IServiceCollection services, string databasePath)
    {
        // Register DbContext with database path
        services.AddSingleton<SqliteDbContext>(provider => new SqliteDbContext(databasePath));
        services.AddSingleton<ISqliteDbContext>(provider => provider.GetRequiredService<SqliteDbContext>());
        
        // Register repositories
        services.AddScoped<IOllamaRepository, SqliteOllamaRepository>();
        services.AddScoped<IUsageMetricsRepository, SqliteUsageMetricsRepository>();
        services.AddScoped<IProviderUsageRepository, SqliteProviderUsageRepository>();
        services.AddScoped<IProviderAccountRepository, SqliteProviderAccountRepository>();
        
        return services;
    }
}