using ModelContextProtocol.Server;
using System.ComponentModel;

namespace Knowledge.Mcp;

/// <summary>
/// Minimal MCP HTTP server for testing - based on working example
/// Run with: dotnet run --project Knowledge.Mcp.csproj -- --minimal-http
/// </summary>
public class MinimalHttpProgram
{
    public static async Task<int> RunMinimalHttp(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Configure URLs explicitly
        builder.WebHost.UseUrls("http://localhost:5000", "http://0.0.0.0:5000");

        // Add CORS for web clients
        builder.Services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                policy.AllowAnyOrigin()
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .WithExposedHeaders("Content-Type", "Cache-Control", "X-Request-Id");
            });
        });

        // Register MCP server and discover tools from the current assembly
        builder.Services.AddMcpServer().WithHttpTransport().WithToolsFromAssembly();

        var app = builder.Build();

        // Enable CORS
        app.UseCors();

        // Add routing middleware (must come before MapMcp)
        app.UseRouting();

        // Add exception handling for debugging
        app.Use(async (context, next) =>
        {
            try
            {
                await next(context);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ HTTP Error: {context.Request.Method} {context.Request.Path}");
                Console.WriteLine($"   {ex.Message}");
                Console.WriteLine($"   {ex.StackTrace}");
                throw;
            }
        });

        // Add MCP middleware
        app.MapMcp();

        Console.WriteLine("Minimal MCP Server started!");
        Console.WriteLine("Server listening on: http://localhost:5000");
        Console.WriteLine("");
        Console.WriteLine("MCP Endpoints:");
        Console.WriteLine("  GET  /sse       - Server-Sent Events stream (for MCP Inspector)");
        Console.WriteLine("  POST /messages  - JSON-RPC requests");
        Console.WriteLine("");
        Console.WriteLine("Test commands:");
        Console.WriteLine("  curl -i http://localhost:5000/sse -H 'Accept: text/event-stream'");
        Console.WriteLine("  curl -X POST http://localhost:5000/messages -H 'Content-Type: application/json' -d '{\"jsonrpc\":\"2.0\",\"id\":1,\"method\":\"initialize\",\"params\":{\"protocolVersion\":\"2024-11-05\",\"capabilities\":{},\"clientInfo\":{\"name\":\"test\",\"version\":\"1.0\"}}}'");

        await app.RunAsync();
        return 0;
    }
}

/// <summary>
/// Simple test tool for the minimal server
/// </summary>
[McpServerToolType]
public sealed class TestTools
{
    [McpServerTool, Description("Says Hello to a user")]
    public static string SayHello(string username)
    {
        return $"Hello, {username}!";
    }

    [McpServerTool, Description("Returns the current server time")]
    public static string GetServerTime()
    {
        return DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
    }
}
