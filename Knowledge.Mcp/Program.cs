using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol;
using ModelContextProtocol.Server;
using OpenTelemetry;
using OpenTelemetry.Trace;
using KnowledgeEngine.Services;
using KnowledgeEngine.Services.HealthCheckers;
using Knowledge.Analytics.Services;
using Knowledge.Data.Extensions;
using KnowledgeEngine.Extensions;
using ChatCompletion.Config;

namespace Knowledge.Mcp;

/// <summary>
/// Knowledge Management MCP Server - Exposes ChatComplete agent functionality via Model Context Protocol
/// </summary>
class Program
{
    static async Task Main(string[] args)
    {
        try
        {
            // Build and run the MCP server host
            var host = CreateHostBuilder(args).Build();

            // Start the MCP server
            await host.RunAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to start Knowledge MCP Server: {ex.Message}");
            Environment.Exit(1);
        }
    }

    static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureServices(
                (context, services) =>
                {
                    // Add configuration
                    var configuration = context.Configuration;
                    var databasePath = configuration["DatabasePath"] ?? "/tmp/knowledge-mcp/knowledge.db";

                    // Create ChatCompleteSettings from configuration
                    var chatCompleteSettings = new ChatCompleteSettings();
                    configuration.GetSection("ChatCompleteSettings").Bind(chatCompleteSettings);
                    
                    // Debug: Check vector store configuration
                    Console.WriteLine($"Debug - VectorStore Provider: {chatCompleteSettings.VectorStore?.Provider}");
                    Console.WriteLine($"Debug - VectorStore is null: {chatCompleteSettings.VectorStore == null}");

                    // Register settings as singleton
                    services.AddSingleton(chatCompleteSettings);

                    // Add OpenTelemetry for observability
                    services
                        .AddOpenTelemetry()
                        .WithTracing(builder =>
                        {
                            builder
                                .AddSource("Knowledge.Mcp")
                                .SetSampler(new AlwaysOnSampler())
                                .AddConsoleExporter()
                                .AddOtlpExporter();
                        });

                    // Configure HTTP client services (required for health checkers)
                    services.AddHttpClient();

                    // Configure database services (required for health checks)
                    services.AddKnowledgeData(databasePath);

                    // Re-enable knowledge services to test vector store configuration
                    services.AddKnowledgeServices(chatCompleteSettings);
                    
                    // Debug: Test what IVectorStoreStrategy is being resolved
                    services.AddScoped<object>(provider =>
                    {
                        try 
                        {
                            var vectorStore = provider.GetService<KnowledgeEngine.Persistence.VectorStores.IVectorStoreStrategy>();
                            Console.WriteLine($"Debug - Resolved IVectorStoreStrategy: {vectorStore?.GetType().Name ?? "null"}");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Debug - Failed to resolve IVectorStoreStrategy: {ex.Message}");
                        }
                        return new object();
                    });

                    // Register system health services and their dependencies
                    services.AddScoped<ISystemHealthService, SystemHealthService>();
                    services.AddScoped<IUsageTrackingService, SqliteUsageTrackingService>();

                    // Register component health checkers (temporarily removing Qdrant for debugging)
                    services.AddScoped<IComponentHealthChecker, SqliteHealthChecker>();
                    services.AddScoped<IComponentHealthChecker, OllamaHealthChecker>();
                    // Temporarily removed: services.AddScoped<IComponentHealthChecker, QdrantHealthChecker>();
                    // Still disabled for testing:
                    // services.AddScoped<IComponentHealthChecker, OpenAIHealthChecker>();
                    // services.AddScoped<IComponentHealthChecker, AnthropicHealthChecker>();
                    // services.AddScoped<IComponentHealthChecker, GoogleAIHealthChecker>();

                    // Configure MCP server with STDIO transport
                    services.AddMcpServer().WithStdioServerTransport().WithToolsFromAssembly();

                    // Configure logging
                    services.AddLogging(builder =>
                    {
                        builder.SetMinimumLevel(LogLevel.Information);
                        builder.AddConsole();
                    });
                }
            );
}
