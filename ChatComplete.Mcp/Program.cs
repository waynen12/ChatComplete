using System.Text.Json;
using System.Text.Json.Nodes;
using ChatComplete.Mcp.Configuration;
using ChatComplete.Mcp.Extensions;
using ChatComplete.Mcp.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ChatComplete.Mcp;

/// <summary>
/// ChatComplete MCP Server - Main entry point
/// </summary>
class Program
{
    private static ILogger<Program>? _logger;
    
    static async Task<int> Main(string[] args)
    {
        try
        {
            Console.WriteLine("🚀 Starting ChatComplete MCP Server...");
            
            // Build configuration
            var configuration = BuildConfiguration(args);
            
            // Build and run host
            var host = CreateHostBuilder(args, configuration).Build();
            
            // Get logger
            _logger = host.Services.GetRequiredService<ILogger<Program>>();
            _logger.LogInformation("🚀 ChatComplete MCP Server starting up...");
            
            // Validate configuration
            var options = configuration.GetSection("McpServer").Get<McpServerOptions>() ?? new McpServerOptions();
            options.ValidateMcpConfiguration();
            
            // Start the server
            await RunMcpServerAsync(host.Services, args);
            
            return 0;
        }
        catch (Exception ex)
        {
            var errorMessage = $"❌ ChatComplete MCP Server failed to start: {ex.Message}";
            
            if (_logger != null)
                _logger.LogCritical(ex, errorMessage);
            else
                Console.WriteLine(errorMessage);
                
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            return 1;
        }
    }
    
    /// <summary>
    /// Create host builder with all services configured
    /// </summary>
    private static IHostBuilder CreateHostBuilder(string[] args, IConfiguration configuration) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureServices((context, services) =>
            {
                // Load MCP server options
                var mcpOptions = configuration.GetSection("McpServer").Get<McpServerOptions>() ?? new McpServerOptions();
                
                // Add MCP server services
                services.AddChatCompleteMcpServer(configuration);
                
                // Add OpenTelemetry
                services.AddMcpOpenTelemetry(mcpOptions);
                
                // Add logging
                services.AddMcpLogging(mcpOptions.Logging);
            });
    
    /// <summary>
    /// Build configuration from appsettings.json and environment variables
    /// </summary>
    private static IConfiguration BuildConfiguration(string[] args)
    {
        return new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables("CHATCOMPLETE_")
            .AddCommandLine(args)
            .Build();
    }
    
    /// <summary>
    /// Run the MCP server in interactive mode
    /// </summary>
    private static async Task RunMcpServerAsync(IServiceProvider services, string[] args)
    {
        var mcpServer = services.GetRequiredService<ChatCompleteMcpServer>();
        var logger = services.GetRequiredService<ILogger<Program>>();
        
        // Display server information
        var serverInfo = mcpServer.GetServerInfo();
        logger.LogInformation("✅ {ServerName} v{Version} ready with {ToolCount} tools", 
            serverInfo.Name, serverInfo.Version, serverInfo.Tools.Count);
        
        // List available tools
        logger.LogInformation("🛠️ Available tools:");
        foreach (var tool in serverInfo.Tools)
        {
            logger.LogInformation("  • {ToolName}: {ToolDescription}", tool.Name, tool.Description);
        }
        
        // Check for different execution modes
        if (args.Contains("--list-tools"))
        {
            await ListToolsAsync(mcpServer);
            return;
        }
        
        if (args.Contains("--health-check"))
        {
            await PerformHealthCheckAsync(mcpServer);
            return;
        }
        
        if (args.Contains("--test-tool"))
        {
            await TestToolAsync(mcpServer, args, logger);
            return;
        }
        
        // Default: Run interactive mode
        await RunInteractiveModeAsync(mcpServer, logger);
    }
    
    /// <summary>
    /// List all available tools and their schemas
    /// </summary>
    private static async Task ListToolsAsync(ChatCompleteMcpServer mcpServer)
    {
        Console.WriteLine("\n📋 Available MCP Tools:\n");
        
        var tools = mcpServer.ListTools();
        
        foreach (var tool in tools)
        {
            Console.WriteLine($"🔧 {tool.Name}");
            Console.WriteLine($"   Description: {tool.Description}");
            
            if (tool.InputSchema != null)
            {
                Console.WriteLine($"   Input Schema:");
                var formattedSchema = JsonSerializer.Serialize(tool.InputSchema, new JsonSerializerOptions { WriteIndented = true });
                Console.WriteLine($"   {formattedSchema}");
            }
            
            Console.WriteLine();
        }
    }
    
    /// <summary>
    /// Perform health check
    /// </summary>
    private static async Task PerformHealthCheckAsync(ChatCompleteMcpServer mcpServer)
    {
        Console.WriteLine("🏥 Performing MCP Server Health Check...\n");
        
        try
        {
            var healthStatus = mcpServer.GetHealthStatus();
            
            Console.WriteLine($"Status: {healthStatus["status"]}");
            Console.WriteLine($"Tools Available: {healthStatus["tools_available"]}");
            Console.WriteLine($"Uptime: {healthStatus["uptime_seconds"]} seconds");
            Console.WriteLine($"Timestamp: {healthStatus["timestamp"]}");
            
            if (healthStatus["tools"] is List<string> tools)
            {
                Console.WriteLine($"Tools: {string.Join(", ", tools)}");
            }
            
            Console.WriteLine("\n✅ Health check completed successfully");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Health check failed: {ex.Message}");
            throw;
        }
    }
    
    /// <summary>
    /// Test a specific tool
    /// </summary>
    private static async Task TestToolAsync(ChatCompleteMcpServer mcpServer, string[] args, ILogger logger)
    {
        var toolNameIndex = Array.IndexOf(args, "--test-tool") + 1;
        if (toolNameIndex >= args.Length)
        {
            Console.WriteLine("❌ Please specify a tool name after --test-tool");
            return;
        }
        
        var toolName = args[toolNameIndex];
        
        Console.WriteLine($"🧪 Testing tool: {toolName}\n");
        
        // Get tool info
        var toolInfo = mcpServer.GetToolInfo(toolName);
        if (toolInfo == null)
        {
            Console.WriteLine($"❌ Tool '{toolName}' not found");
            return;
        }
        
        // Execute tool with empty parameters (for basic test)
        var testParameters = new JsonObject();
        
        try
        {
            var result = await mcpServer.ExecuteToolAsync(toolName, testParameters);
            
            if (result.IsSuccess)
            {
                Console.WriteLine("✅ Tool execution successful:");
                Console.WriteLine($"Duration: {result.Duration?.TotalMilliseconds:F2}ms");
                Console.WriteLine($"Content: {result.Content}");
                
                if (result.Metadata.Any())
                {
                    Console.WriteLine("Metadata:");
                    foreach (var kvp in result.Metadata)
                    {
                        Console.WriteLine($"  {kvp.Key}: {kvp.Value}");
                    }
                }
            }
            else
            {
                Console.WriteLine($"❌ Tool execution failed: {result.ErrorMessage}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Tool test exception: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Run interactive mode (placeholder for actual MCP protocol implementation)
    /// </summary>
    private static async Task RunInteractiveModeAsync(ChatCompleteMcpServer mcpServer, ILogger logger)
    {
        logger.LogInformation("🖥️ Starting interactive mode. Press Ctrl+C to exit.");
        logger.LogInformation("💡 Use command line arguments for specific operations:");
        logger.LogInformation("   --list-tools     : List all available tools");
        logger.LogInformation("   --health-check   : Perform health check");
        logger.LogInformation("   --test-tool <name> : Test a specific tool");
        
        Console.WriteLine("\n🔗 MCP Server Ready for Connections");
        Console.WriteLine("📡 External systems can now connect to this server via MCP protocol");
        Console.WriteLine("🌐 Grafana, Claude Desktop, VS Code, and other MCP clients can use these tools:");
        
        var tools = mcpServer.ListTools();
        foreach (var tool in tools)
        {
            Console.WriteLine($"  • mcp://{tool.Name} - {tool.Description}");
        }
        
        Console.WriteLine("\n⚠️  Note: Full MCP protocol implementation would be added here");
        Console.WriteLine("   This demonstrates the MCP server functionality with direct tool execution");
        
        // Keep the server running
        var cancellationToken = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            cancellationToken.Cancel();
            logger.LogInformation("🛑 Shutting down MCP server...");
        };
        
        try
        {
            await Task.Delay(-1, cancellationToken.Token);
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("✅ MCP server stopped gracefully");
        }
    }
}