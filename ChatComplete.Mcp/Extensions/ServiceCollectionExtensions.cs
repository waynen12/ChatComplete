#pragma warning disable SKEXP0001, SKEXP0010, SKEXP0020, SKEXP0050

using ChatComplete.Mcp.Configuration;
using ChatComplete.Mcp.Interfaces;
using ChatComplete.Mcp.Services;
using ChatComplete.Mcp.Tools;
using ChatCompletion.Config;
using Knowledge.Analytics.Extensions;
using Knowledge.Analytics.Services;
using Knowledge.Data.Extensions;
using KnowledgeEngine.Agents.Plugins;
using KnowledgeEngine.Extensions;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenTelemetry;

namespace ChatComplete.Mcp.Extensions;

/// <summary>
/// Service collection extensions for registering MCP server dependencies
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Add ChatComplete MCP Server with all dependencies
    /// </summary>
    public static IServiceCollection AddChatCompleteMcpServer(this IServiceCollection services, IConfiguration configuration)
    {
        // Register configuration
        services.Configure<McpServerOptions>(configuration.GetSection("McpServer"));
        
        // Add our existing services that agents depend on
        var databasePath = configuration.GetConnectionString("DefaultConnection") ?? "data/knowledge.db";
        services.AddKnowledgeData(databasePath);
        services.AddAnalyticsServices(configuration);
        
        // Add ChatCompleteSettings for vector store configuration
        var settings = configuration.GetSection("ChatCompleteSettings").Get<ChatCompleteSettings>() 
                      ?? new ChatCompleteSettings(); // Provide default if missing
        services.AddSingleton(settings);
        
        // Configure embedding services - simplified for MCP server (just use OpenAI with dummy key)
        var openAiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? "dummy-key-for-mcp-testing";
        services.AddOpenAIEmbeddingGenerator("text-embedding-ada-002", openAiKey);
        
        // Add SQLite persistence (needed for Qdrant deployments)
        services.AddSqlitePersistence(settings);
        
        // Add knowledge services (including vector store strategies)
        services.AddKnowledgeServices(settings);
        
        // Add required services for health checking
        services.AddScoped<KnowledgeEngine.Services.ISystemHealthService, KnowledgeEngine.Services.SystemHealthService>();
        
        // Add individual health checkers (they all implement IComponentHealthChecker)
        services.AddScoped<KnowledgeEngine.Services.HealthCheckers.IComponentHealthChecker, KnowledgeEngine.Services.HealthCheckers.SqliteHealthChecker>();
        services.AddScoped<KnowledgeEngine.Services.HealthCheckers.IComponentHealthChecker, KnowledgeEngine.Services.HealthCheckers.QdrantHealthChecker>();
        services.AddScoped<KnowledgeEngine.Services.HealthCheckers.IComponentHealthChecker, KnowledgeEngine.Services.HealthCheckers.OllamaHealthChecker>();
        services.AddScoped<KnowledgeEngine.Services.HealthCheckers.IComponentHealthChecker, KnowledgeEngine.Services.HealthCheckers.OpenAIHealthChecker>();
        services.AddScoped<KnowledgeEngine.Services.HealthCheckers.IComponentHealthChecker, KnowledgeEngine.Services.HealthCheckers.AnthropicHealthChecker>();
        services.AddScoped<KnowledgeEngine.Services.HealthCheckers.IComponentHealthChecker, KnowledgeEngine.Services.HealthCheckers.GoogleAIHealthChecker>();
        
        // Register our existing agent plugins
        services.AddScoped<SystemHealthAgent>();
        services.AddScoped<CrossKnowledgeSearchPlugin>();
        services.AddScoped<ModelRecommendationAgent>();
        services.AddScoped<KnowledgeAnalyticsAgent>();
        
        // Register MCP tool providers
        services.AddScoped<IMcpToolProvider, SystemHealthMcpTool>();
        services.AddScoped<IMcpToolProvider, ComponentHealthMcpTool>();
        services.AddScoped<IMcpToolProvider, SystemMetricsMcpTool>();
        services.AddScoped<IMcpToolProvider, CrossKnowledgeSearchMcpTool>();
        services.AddScoped<IMcpToolProvider, ModelRecommendationMcpTool>();
        services.AddScoped<IMcpToolProvider, ModelPerformanceMcpTool>();
        services.AddScoped<IMcpToolProvider, ModelComparisonMcpTool>();
        services.AddScoped<IMcpToolProvider, KnowledgeAnalyticsMcpTool>();
        
        // Register MCP server
        services.AddScoped<ChatCompleteMcpServer>();
        
        return services;
    }
    
    /// <summary>
    /// Add OpenTelemetry with MCP server configuration
    /// </summary>
    public static IServiceCollection AddMcpOpenTelemetry(this IServiceCollection services, McpServerOptions options)
    {
        services.AddOpenTelemetry()
            .WithTracing(tracing => tracing.ConfigureTracing(options))
            .WithMetrics(metrics => metrics.ConfigureMetrics(options));
            
        return services;
    }
    
    /// <summary>
    /// Add logging configuration for MCP server
    /// </summary>
    public static IServiceCollection AddMcpLogging(this IServiceCollection services, LoggingOptions loggingOptions)
    {
        services.AddLogging(builder =>
        {
            builder.ClearProviders();
            
            // Console logging
            builder.AddConsole(options =>
            {
                options.IncludeScopes = true;
                options.TimestampFormat = "[yyyy-MM-dd HH:mm:ss] ";
            });
            
            // Set log level
            if (Enum.TryParse<LogLevel>(loggingOptions.LogLevel, out var logLevel))
            {
                builder.SetMinimumLevel(logLevel);
            }
            
            // File logging if configured
            if (!string.IsNullOrEmpty(loggingOptions.LogFilePath))
            {
                // Note: Would need to add Serilog or similar for file logging
                // For now, just console logging
            }
        });
        
        return services;
    }
    
    /// <summary>
    /// Validate MCP server configuration
    /// </summary>
    public static void ValidateMcpConfiguration(this McpServerOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(options.OpenTelemetry);
        ArgumentNullException.ThrowIfNull(options.Logging);
        ArgumentNullException.ThrowIfNull(options.Security);
        
        // Validate OpenTelemetry configuration
        if (!string.IsNullOrEmpty(options.OpenTelemetry.OtlpEndpoint))
        {
            if (!Uri.TryCreate(options.OpenTelemetry.OtlpEndpoint, UriKind.Absolute, out _))
            {
                throw new ArgumentException($"Invalid OTLP endpoint URL: {options.OpenTelemetry.OtlpEndpoint}");
            }
        }
        
        // Validate trace sample rate
        if (options.OpenTelemetry.TraceSampleRate < 0.0 || options.OpenTelemetry.TraceSampleRate > 1.0)
        {
            throw new ArgumentException("TraceSampleRate must be between 0.0 and 1.0");
        }
        
        // Validate security settings
        if (options.Security.EnableRateLimiting && options.Security.MaxRequestsPerMinute <= 0)
        {
            throw new ArgumentException("MaxRequestsPerMinute must be greater than 0 when rate limiting is enabled");
        }
        
        // Validate logging settings
        if (!Enum.TryParse<LogLevel>(options.Logging.LogLevel, out _))
        {
            throw new ArgumentException($"Invalid log level: {options.Logging.LogLevel}");
        }
    }
}