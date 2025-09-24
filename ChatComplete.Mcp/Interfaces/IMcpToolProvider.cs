using System.Text.Json.Nodes;
using ChatComplete.Mcp.Models;

namespace ChatComplete.Mcp.Interfaces;

/// <summary>
/// Interface for MCP-compatible tool providers that wrap our existing agent functionality
/// </summary>
public interface IMcpToolProvider
{
    /// <summary>
    /// Tool name - must be unique across all providers
    /// </summary>
    string Name { get; }
    
    /// <summary>
    /// Human-readable tool description
    /// </summary>
    string Description { get; }
    
    /// <summary>
    /// JSON schema defining the tool's input parameters
    /// </summary>
    JsonObject InputSchema { get; }
    
    /// <summary>
    /// Execute the tool with the provided parameters
    /// </summary>
    /// <param name="parameters">Tool input parameters as JSON</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Tool execution result</returns>
    Task<McpToolResult> ExecuteAsync(JsonObject parameters, CancellationToken cancellationToken = default);
}