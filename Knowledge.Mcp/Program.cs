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
using Knowledge.Mcp.Configuration;

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
        
        // Check if we should run minimal MCP server
        if (args.Length > 0 && args[0] == "--minimal")
        {
            return await MinimalProgram.RunMinimal(args);
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
            .ConfigureAppConfiguration((context, config) =>
            {
                // Set the base path to the directory where the executable is located
                // This ensures appsettings.json is found regardless of working directory
                var basePath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
                config.SetBasePath(basePath!);

                // Clear existing sources and add our configuration files
                config.Sources.Clear();
                config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
                config.AddEnvironmentVariables();
                config.AddCommandLine(args);

                Console.WriteLine($"MCP Server configuration base path: {basePath}");
            })
            .ConfigureServices(
                (context, services) =>
                {
                    
                    // Add configuration
                    var configuration = context.Configuration;
                    // Create ChatCompleteSettings from configuration
                    var chatCompleteSettings = new ChatCompleteSettings();
                    configuration.GetSection("ChatCompleteSettings").Bind(chatCompleteSettings);
                    var databasePath = chatCompleteSettings.DatabasePath;

                    if (string.IsNullOrWhiteSpace(databasePath))
                    {
                        Console.WriteLine("WARNING: DatabasePath not configured in appsettings.json.");
                        Console.WriteLine("This likely means appsettings.json was not copied to the output directory.");
                        throw new Exception("DatabasePath not configured in appsettings.json.");
                    }

                    Console.WriteLine($"MCP Server using database path: {databasePath}");


                    
                    // Force correct Qdrant configuration (Bind() doesn't override defaults properly)
                    chatCompleteSettings.VectorStore.Provider = "Qdrant";
                    chatCompleteSettings.VectorStore.Qdrant.Port = 6334;

                    // Register settings as singleton
                    services.AddSingleton(chatCompleteSettings);

                    // Register MCP server specific settings
                    var mcpServerSettings = new McpServerSettings();
                    configuration.GetSection(McpServerSettings.SectionName).Bind(mcpServerSettings);
                    services.AddSingleton(mcpServerSettings);

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

                    // Register knowledge management services (required for search and analytics MCP tools)
                    
                    // Register KnowledgeEngine SqliteDbContext (different from Knowledge.Data one)
                    services.AddScoped<KnowledgeEngine.Persistence.Sqlite.SqliteDbContext>(provider =>
                    {
                        return new KnowledgeEngine.Persistence.Sqlite.SqliteDbContext(databasePath);
                    });
                    
                    services.AddScoped<KnowledgeEngine.Persistence.IKnowledgeRepository, KnowledgeEngine.Persistence.Sqlite.Repositories.SqliteKnowledgeRepository>();
                    
                    // Register embedding services (required for knowledge search)
                    // Use Ollama embedding service (configured in appsettings.json)
                    var activeProvider = chatCompleteSettings.EmbeddingProviders?.ActiveProvider ?? "Ollama";

                    if (activeProvider == "Ollama")
                    {
                        var ollamaSettings = chatCompleteSettings.EmbeddingProviders?.Ollama;
                        var ollamaBaseUrl = chatCompleteSettings.OllamaBaseUrl ?? "http://localhost:11434";
                        var modelName = ollamaSettings?.ModelName ?? "nomic-embed-text";

                        Console.WriteLine($"MCP Server configuring Ollama embedding: {ollamaBaseUrl} with model {modelName}");

                        services.AddScoped<Microsoft.Extensions.AI.IEmbeddingGenerator<string, Microsoft.Extensions.AI.Embedding<float>>>(provider =>
                        {
                            var httpClient = provider.GetRequiredService<IHttpClientFactory>().CreateClient();
                            return new Knowledge.Mcp.Services.OllamaEmbeddingService(httpClient, ollamaBaseUrl, modelName);
                        });
                    }
                    else
                    {
                        throw new NotSupportedException(
                            $"Embedding provider '{activeProvider}' not supported in MCP server. " +
                            "Currently only Ollama is supported. Set ActiveProvider to 'Ollama' in appsettings.json");
                    }
                    
                    // Register KnowledgeManager (required for CrossKnowledgeSearchMcpTool)
                    services.AddScoped<KnowledgeEngine.KnowledgeManager>();
                    
                    // Register agent plugins (required for MCP tool implementations)
                    services.AddScoped<KnowledgeEngine.Agents.Plugins.CrossKnowledgeSearchPlugin>();
                    services.AddScoped<KnowledgeEngine.Agents.Plugins.ModelRecommendationAgent>();
                    services.AddScoped<KnowledgeEngine.Agents.Plugins.KnowledgeAnalyticsAgent>();

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
