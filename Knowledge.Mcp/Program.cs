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
    static async Task<int> Main(string[] args)
    {
        // Check if we should run the test program instead
        if (args.Length > 0 && args[0] == "--test-collections")
        {
            return await TestQdrantCollections.RunTest(args);
        }

        try
        {
            // Build and run the MCP server host
            var host = CreateHostBuilder(args).Build();

            // Start the MCP server
            await host.RunAsync();
            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to start Knowledge MCP Server: {ex.Message}");
            return 1;
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

                    // Create ChatCompleteSettings from configuration (same pattern as main API)
                    var chatCompleteSettings = configuration.GetSection("ChatCompleteSettings").Get<ChatCompleteSettings>()
                        ?? throw new InvalidOperationException("ChatCompleteSettings configuration is missing or invalid");

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

                    // Register Qdrant-only services (bypass MongoDB completely)
                    
                    // Register QdrantSettings
                    services.AddSingleton(chatCompleteSettings.VectorStore.Qdrant);
                    
                    // Register Qdrant services directly
                    services.AddSingleton<Microsoft.SemanticKernel.Connectors.Qdrant.QdrantVectorStore>(provider =>
                    {
                        var qdrantSettings = chatCompleteSettings.VectorStore.Qdrant;
                        var qdrantClient = new Qdrant.Client.QdrantClient(
                            host: qdrantSettings.Host,
                            port: qdrantSettings.Port,
                            https: qdrantSettings.UseHttps,
                            apiKey: qdrantSettings.ApiKey
                        );
                        return new Microsoft.SemanticKernel.Connectors.Qdrant.QdrantVectorStore(qdrantClient, ownsClient: true);
                    });
                    
                    // Register Qdrant Index Manager
                    services.AddScoped<KnowledgeEngine.Persistence.IndexManagers.IIndexManager, KnowledgeEngine.Persistence.IndexManagers.QdrantIndexManager>();
                    
                    // Register Qdrant Vector Store Strategy
                    services.AddScoped<KnowledgeEngine.Persistence.VectorStores.IVectorStoreStrategy, KnowledgeEngine.Persistence.VectorStores.QdrantVectorStoreStrategy>(provider =>
                    {
                        var vectorStore = provider.GetRequiredService<Microsoft.SemanticKernel.Connectors.Qdrant.QdrantVectorStore>();
                        var qdrantSettings = provider.GetRequiredService<ChatCompletion.Config.QdrantSettings>();
                        var indexManager = provider.GetRequiredService<KnowledgeEngine.Persistence.IndexManagers.IIndexManager>();
                        return new KnowledgeEngine.Persistence.VectorStores.QdrantVectorStoreStrategy(vectorStore, qdrantSettings, indexManager, chatCompleteSettings);
                    });

                    // Register system health services and their dependencies
                    services.AddScoped<ISystemHealthService, SystemHealthService>();
                    services.AddScoped<IUsageTrackingService, SqliteUsageTrackingService>();

                    // Register component health checkers (Qdrant-only, no MongoDB)
                    services.AddScoped<IComponentHealthChecker, SqliteHealthChecker>();
                    services.AddScoped<IComponentHealthChecker, OllamaHealthChecker>();
                    services.AddScoped<IComponentHealthChecker, QdrantHealthChecker>(); // Should work now with pure Qdrant setup
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
