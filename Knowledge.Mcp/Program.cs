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
                    var databasePath = configuration["DatabasePath"] ?? "data/knowledge.db";

                    // Create ChatCompleteSettings from configuration
                    var chatCompleteSettings = new ChatCompleteSettings();
                    configuration.GetSection("ChatCompleteSettings").Bind(chatCompleteSettings);

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

                    // Register knowledge services (includes vector store strategy)
                    services.AddKnowledgeServices(chatCompleteSettings);

                    // Register system health services and their dependencies
                    services.AddScoped<ISystemHealthService, SystemHealthService>();
                    services.AddScoped<IUsageTrackingService, SqliteUsageTrackingService>();

                    // Register all component health checkers
                    services.AddScoped<IComponentHealthChecker, SqliteHealthChecker>();
                    services.AddScoped<IComponentHealthChecker, QdrantHealthChecker>();
                    services.AddScoped<IComponentHealthChecker, OpenAIHealthChecker>();
                    services.AddScoped<IComponentHealthChecker, AnthropicHealthChecker>();
                    services.AddScoped<IComponentHealthChecker, GoogleAIHealthChecker>();
                    services.AddScoped<IComponentHealthChecker, OllamaHealthChecker>();

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
