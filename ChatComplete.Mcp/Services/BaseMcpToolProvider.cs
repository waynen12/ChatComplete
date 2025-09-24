using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Text.Json.Nodes;
using ChatComplete.Mcp.Interfaces;
using ChatComplete.Mcp.Models;
using Microsoft.Extensions.Logging;

namespace ChatComplete.Mcp.Services;

/// <summary>
/// Base class for MCP tool providers with OpenTelemetry instrumentation
/// </summary>
public abstract class BaseMcpToolProvider : IMcpToolProvider
{
    private static readonly ActivitySource ActivitySource = new("ChatComplete.Mcp");
    private static readonly Meter Meter = new("ChatComplete.Mcp");
    
    protected readonly ILogger Logger;
    
    // Metrics
    private readonly Counter<int> _executionCounter;
    private readonly Histogram<double> _executionDuration;
    
    protected BaseMcpToolProvider(ILogger logger)
    {
        Logger = logger;
        
        // Initialize metrics
        _executionCounter = Meter.CreateCounter<int>(
            name: "mcp_tool_executions_total",
            description: "Total number of MCP tool executions"
        );
        
        _executionDuration = Meter.CreateHistogram<double>(
            name: "mcp_tool_execution_duration_seconds", 
            description: "Duration of MCP tool executions in seconds"
        );
    }
    
    public abstract string Name { get; }
    public abstract string Description { get; }
    public abstract JsonObject InputSchema { get; }
    
    /// <summary>
    /// Execute the tool with OpenTelemetry instrumentation
    /// </summary>
    public async Task<McpToolResult> ExecuteAsync(JsonObject parameters, CancellationToken cancellationToken = default)
    {
        using var activity = ActivitySource.StartActivity($"MCP.{Name}");
        activity?.SetTag("tool.name", Name);
        activity?.SetTag("tool.parameters_count", parameters.Count.ToString());
        
        var stopwatch = Stopwatch.StartNew();
        var tags = new TagList();
        tags.Add("tool", Name);
        
        try
        {
            Logger.LogInformation("🔧 Executing MCP tool: {ToolName} with {ParameterCount} parameters", 
                Name, parameters.Count);
            
            // Add parameter details to trace (but sanitize sensitive data)
            foreach (var param in parameters)
            {
                if (!IsSensitiveParameter(param.Key))
                {
                    activity?.SetTag($"param.{param.Key}", param.Value?.ToString());
                }
            }
            
            // Execute the actual tool logic
            var result = await ExecuteToolAsync(parameters, cancellationToken);
            
            stopwatch.Stop();
            result.Duration = stopwatch.Elapsed;
            result.TraceId = activity?.Id;
            
            // Record success metrics
            tags.Add("status", result.IsSuccess ? "success" : "error");
            _executionCounter.Add(1, tags);
            _executionDuration.Record(stopwatch.Elapsed.TotalSeconds, tags);
            
            // Add result metadata to trace
            activity?.SetTag("result.success", result.IsSuccess);
            if (result.IsSuccess)
            {
                activity?.SetTag("result.content_length", result.Content?.ToString()?.Length ?? 0);
                activity?.SetStatus(ActivityStatusCode.Ok);
                
                Logger.LogInformation("✅ MCP tool {ToolName} completed successfully in {Duration}ms", 
                    Name, stopwatch.ElapsedMilliseconds);
            }
            else
            {
                activity?.SetTag("result.error", result.ErrorMessage);
                activity?.SetStatus(ActivityStatusCode.Error, result.ErrorMessage);
                
                Logger.LogWarning("⚠️ MCP tool {ToolName} failed: {Error}", 
                    Name, result.ErrorMessage);
            }
            
            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            
            // Record error metrics
            tags.Add("status", "exception");
            _executionCounter.Add(1, tags);
            _executionDuration.Record(stopwatch.Elapsed.TotalSeconds, tags);
            
            // Add error details to trace
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.SetTag("exception.type", ex.GetType().Name);
            activity?.SetTag("exception.message", ex.Message);
            
            Logger.LogError(ex, "❌ MCP tool {ToolName} threw exception: {Message}", 
                Name, ex.Message);
            
            return McpToolResult.Error($"Tool execution failed: {ex.Message}", ex);
        }
    }
    
    /// <summary>
    /// Implement the actual tool logic in derived classes
    /// </summary>
    protected abstract Task<McpToolResult> ExecuteToolAsync(JsonObject parameters, CancellationToken cancellationToken);
    
    /// <summary>
    /// Check if a parameter contains sensitive data that shouldn't be traced
    /// </summary>
    protected virtual bool IsSensitiveParameter(string parameterName)
    {
        var sensitiveParams = new[] { "password", "token", "key", "secret", "auth" };
        return sensitiveParams.Any(sensitive => 
            parameterName.Contains(sensitive, StringComparison.OrdinalIgnoreCase));
    }
    
    /// <summary>
    /// Helper to get string parameter from JSON
    /// </summary>
    protected string? GetStringParameter(JsonObject parameters, string name, string? defaultValue = null)
    {
        return parameters.TryGetPropertyValue(name, out var value) 
            ? value?.ToString() 
            : defaultValue;
    }
    
    /// <summary>
    /// Helper to get boolean parameter from JSON
    /// </summary>
    protected bool GetBoolParameter(JsonObject parameters, string name, bool defaultValue = false)
    {
        if (!parameters.TryGetPropertyValue(name, out var value)) 
            return defaultValue;
            
        return value switch
        {
            JsonValue jsonValue when jsonValue.TryGetValue<bool>(out var boolVal) => boolVal,
            JsonValue jsonValue when jsonValue.TryGetValue<string>(out var stringVal) => 
                bool.TryParse(stringVal, out var parsed) && parsed,
            _ => defaultValue
        };
    }
    
    /// <summary>
    /// Helper to get integer parameter from JSON
    /// </summary>
    protected int GetIntParameter(JsonObject parameters, string name, int defaultValue = 0)
    {
        if (!parameters.TryGetPropertyValue(name, out var value)) 
            return defaultValue;
            
        return value switch
        {
            JsonValue jsonValue when jsonValue.TryGetValue<int>(out var intVal) => intVal,
            JsonValue jsonValue when jsonValue.TryGetValue<string>(out var stringVal) => 
                int.TryParse(stringVal, out var parsed) ? parsed : defaultValue,
            _ => defaultValue
        };
    }
    
    /// <summary>
    /// Helper to get double parameter from JSON
    /// </summary>
    protected double GetDoubleParameter(JsonObject parameters, string name, double defaultValue = 0.0)
    {
        if (!parameters.TryGetPropertyValue(name, out var value)) 
            return defaultValue;
            
        return value switch
        {
            JsonValue jsonValue when jsonValue.TryGetValue<double>(out var doubleVal) => doubleVal,
            JsonValue jsonValue when jsonValue.TryGetValue<string>(out var stringVal) => 
                double.TryParse(stringVal, out var parsed) ? parsed : defaultValue,
            _ => defaultValue
        };
    }
}