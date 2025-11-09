using System.ComponentModel;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ModelContextProtocol.Server;

namespace Knowledge.Mcp;

/// <summary>
/// Minimal MCP server to test basic functionality
/// </summary>
public static class MinimalProgram
{
    public static async Task<int> RunMinimal(string[] args)
    {
        try
        {
            var builder = Host.CreateDefaultBuilder(args);

            builder.ConfigureServices(services =>
            {
                // Add MCP server with minimal configuration
                services.AddMcpServer().WithStdioServerTransport().WithToolsFromAssembly();

                // No complex dependencies
            });

            var host = builder.Build();
            await host.RunAsync();
            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Minimal MCP server failed: {ex.Message}");
            return 1;
        }
    }
}

[ModelContextProtocol.Server.McpServerToolType]
public sealed class MinimalTestTool
{
    [ModelContextProtocol.Server.McpServerTool]
    [Description("Simple test tool to verify MCP functionality")]
    public static Task<string> TestAsync()
    {
        var result = new
        {
            Status = "Working",
            Timestamp = DateTime.UtcNow,
            Message = "Minimal MCP server is responding correctly",
        };

        return Task.FromResult(
            JsonSerializer.Serialize(
                result,
                new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                }
            )
        );
    }
}
