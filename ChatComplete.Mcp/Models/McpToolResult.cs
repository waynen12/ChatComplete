using System.Text.Json.Nodes;

namespace ChatComplete.Mcp.Models;

/// <summary>
/// Result of MCP tool execution
/// </summary>
public class McpToolResult
{
    /// <summary>
    /// Whether the tool execution was successful
    /// </summary>
    public bool IsSuccess { get; set; } = true;
    
    /// <summary>
    /// Main content result (text or structured data)
    /// </summary>
    public object? Content { get; set; }
    
    /// <summary>
    /// Error message if execution failed
    /// </summary>
    public string? ErrorMessage { get; set; }
    
    /// <summary>
    /// Additional metadata about the execution
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
    
    /// <summary>
    /// Execution duration
    /// </summary>
    public TimeSpan? Duration { get; set; }
    
    /// <summary>
    /// OpenTelemetry trace ID for correlation
    /// </summary>
    public string? TraceId { get; set; }
    
    /// <summary>
    /// Create a successful result
    /// </summary>
    public static McpToolResult Success(object? content, Dictionary<string, object>? metadata = null)
    {
        return new McpToolResult
        {
            IsSuccess = true,
            Content = content,
            Metadata = metadata ?? new Dictionary<string, object>()
        };
    }
    
    /// <summary>
    /// Create an error result
    /// </summary>
    public static McpToolResult Error(string errorMessage, Exception? exception = null)
    {
        var result = new McpToolResult
        {
            IsSuccess = false,
            ErrorMessage = errorMessage
        };
        
        if (exception != null)
        {
            result.Metadata["exception_type"] = exception.GetType().Name;
            result.Metadata["stack_trace"] = exception.StackTrace ?? "";
        }
        
        return result;
    }
}

/// <summary>
/// MCP server information
/// </summary>
public class McpServerInfo
{
    public string Name { get; set; } = "";
    public string Version { get; set; } = "";
    public string Description { get; set; } = "";
    public List<McpToolInfo> Tools { get; set; } = new();
}

/// <summary>
/// Information about an available MCP tool
/// </summary>
public class McpToolInfo
{
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public JsonObject? InputSchema { get; set; }
}