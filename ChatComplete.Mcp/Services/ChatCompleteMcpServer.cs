using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Text.Json.Nodes;
using ChatComplete.Mcp.Interfaces;
using ChatComplete.Mcp.Models;
using Microsoft.Extensions.Logging;

namespace ChatComplete.Mcp.Services;

/// <summary>
/// ChatComplete MCP Server - Main server implementation handling tool registration and execution
/// </summary>
public class ChatCompleteMcpServer
{
    private static readonly ActivitySource ActivitySource = new("ChatComplete.Mcp.Server");
    private static readonly Meter Meter = new("ChatComplete.Mcp.Server");
    
    private readonly ILogger<ChatCompleteMcpServer> _logger;
    private readonly Dictionary<string, IMcpToolProvider> _tools;
    
    // Metrics
    private readonly Counter<int> _requestCounter;
    private readonly Histogram<double> _requestDuration;
    private readonly Counter<int> _toolExecutionCounter;
    
    public ChatCompleteMcpServer(ILogger<ChatCompleteMcpServer> logger, IEnumerable<IMcpToolProvider> tools)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _tools = (tools ?? throw new ArgumentNullException(nameof(tools)))
            .ToDictionary(tool => tool.Name, tool => tool);
            
        // Initialize metrics
        _requestCounter = Meter.CreateCounter<int>(
            name: "mcp_server_requests_total",
            description: "Total number of MCP server requests"
        );
        
        _requestDuration = Meter.CreateHistogram<double>(
            name: "mcp_server_request_duration_seconds",
            description: "Duration of MCP server requests in seconds"
        );
        
        _toolExecutionCounter = Meter.CreateCounter<int>(
            name: "mcp_server_tool_executions_total", 
            description: "Total number of tool executions by the MCP server"
        );
        
        _logger.LogInformation("🚀 ChatComplete MCP Server initialized with {ToolCount} tools: {ToolNames}", 
            _tools.Count, string.Join(", ", _tools.Keys));
    }

    /// <summary>
    /// Get server information and available tools
    /// </summary>
    public McpServerInfo GetServerInfo()
    {
        using var activity = ActivitySource.StartActivity("MCP.GetServerInfo");
        activity?.SetTag("server.name", "chatcomplete");
        activity?.SetTag("tools.count", _tools.Count);
        
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            var serverInfo = new McpServerInfo
            {
                Name = "chatcomplete",
                Version = "1.0.0",
                Description = "AI Knowledge Manager with system intelligence tools - provides health monitoring, model analytics, knowledge base management, and cross-knowledge search capabilities",
                Tools = _tools.Values.Select(tool => new McpToolInfo
                {
                    Name = tool.Name,
                    Description = tool.Description,
                    InputSchema = tool.InputSchema
                }).ToList()
            };
            
            stopwatch.Stop();
            
            // Record metrics
            var successTags = new TagList();
            successTags.Add("operation", "get_server_info");
            successTags.Add("status", "success");
            _requestCounter.Add(1, successTags);
            
            var durationTags = new TagList();
            durationTags.Add("operation", "get_server_info");
            _requestDuration.Record(stopwatch.Elapsed.TotalSeconds, durationTags);
            
            _logger.LogInformation("✅ MCP Server: Provided server info with {ToolCount} available tools", _tools.Count);
            
            return serverInfo;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            var errorTags = new TagList();
            errorTags.Add("operation", "get_server_info");
            errorTags.Add("status", "error");
            _requestCounter.Add(1, errorTags);
            
            var errorDurationTags = new TagList();
            errorDurationTags.Add("operation", "get_server_info");
            _requestDuration.Record(stopwatch.Elapsed.TotalSeconds, errorDurationTags);
            
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            _logger.LogError(ex, "❌ MCP Server: Failed to get server info");
            
            throw;
        }
    }

    /// <summary>
    /// Execute a tool by name with provided parameters
    /// </summary>
    public async Task<McpToolResult> ExecuteToolAsync(string toolName, JsonObject parameters, CancellationToken cancellationToken = default)
    {
        using var activity = ActivitySource.StartActivity("MCP.ExecuteTool");
        activity?.SetTag("tool.name", toolName);
        activity?.SetTag("parameters.count", parameters.Count);
        
        var stopwatch = Stopwatch.StartNew();
        var tags = new TagList();
        tags.Add("tool", toolName);
        
        try
        {
            _logger.LogInformation("🔧 MCP Server: Executing tool '{ToolName}' with {ParameterCount} parameters", 
                toolName, parameters.Count);

            // Check if tool exists
            if (!_tools.TryGetValue(toolName, out var tool))
            {
                var availableTools = string.Join(", ", _tools.Keys);
                var errorMessage = $"Tool '{toolName}' not found. Available tools: {availableTools}";
                
                _logger.LogWarning("⚠️ MCP Server: Tool not found - {ToolName}. Available: {AvailableTools}", 
                    toolName, availableTools);
                
                stopwatch.Stop();
                tags.Add("status", "not_found");
                _requestCounter.Add(1, tags);
                _requestDuration.Record(stopwatch.Elapsed.TotalSeconds, tags);
                
                activity?.SetTag("error.type", "tool_not_found");
                activity?.SetStatus(ActivityStatusCode.Error, errorMessage);
                
                return McpToolResult.Error(errorMessage);
            }

            // Execute the tool
            var result = await tool.ExecuteAsync(parameters, cancellationToken);
            
            stopwatch.Stop();
            
            // Record metrics
            tags.Add("status", result.IsSuccess ? "success" : "error");
            _requestCounter.Add(1, tags);
            _requestDuration.Record(stopwatch.Elapsed.TotalSeconds, tags);
            var toolTags = new TagList();
            toolTags.Add("tool", toolName);
            toolTags.Add("status", result.IsSuccess ? "success" : "error");
            _toolExecutionCounter.Add(1, toolTags);
            
            // Update trace
            activity?.SetTag("result.success", result.IsSuccess);
            if (result.IsSuccess)
            {
                activity?.SetStatus(ActivityStatusCode.Ok);
                _logger.LogInformation("✅ MCP Server: Tool '{ToolName}' executed successfully in {Duration}ms", 
                    toolName, stopwatch.ElapsedMilliseconds);
            }
            else
            {
                activity?.SetTag("result.error", result.ErrorMessage);
                activity?.SetStatus(ActivityStatusCode.Error, result.ErrorMessage);
                _logger.LogWarning("⚠️ MCP Server: Tool '{ToolName}' execution failed: {Error}", 
                    toolName, result.ErrorMessage);
            }
            
            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            
            // Record error metrics
            tags.Add("status", "exception");
            _requestCounter.Add(1, tags);
            _requestDuration.Record(stopwatch.Elapsed.TotalSeconds, tags);
            var exceptionTags = new TagList();
            exceptionTags.Add("tool", toolName);
            exceptionTags.Add("status", "exception");
            _toolExecutionCounter.Add(1, exceptionTags);
            
            // Update trace
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.SetTag("exception.type", ex.GetType().Name);
            activity?.SetTag("exception.message", ex.Message);
            
            _logger.LogError(ex, "❌ MCP Server: Exception executing tool '{ToolName}': {Message}", 
                toolName, ex.Message);
            
            return McpToolResult.Error($"Internal error executing tool: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// List all available tools with their information
    /// </summary>
    public List<McpToolInfo> ListTools()
    {
        using var activity = ActivitySource.StartActivity("MCP.ListTools");
        activity?.SetTag("tools.count", _tools.Count);
        
        try
        {
            var toolInfos = _tools.Values.Select(tool => new McpToolInfo
            {
                Name = tool.Name,
                Description = tool.Description,
                InputSchema = tool.InputSchema
            }).ToList();
            
            _logger.LogInformation("📋 MCP Server: Listed {ToolCount} available tools", _tools.Count);
            
            return toolInfos;
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            _logger.LogError(ex, "❌ MCP Server: Failed to list tools");
            throw;
        }
    }

    /// <summary>
    /// Get information about a specific tool
    /// </summary>
    public McpToolInfo? GetToolInfo(string toolName)
    {
        using var activity = ActivitySource.StartActivity("MCP.GetToolInfo");
        activity?.SetTag("tool.name", toolName);
        
        try
        {
            if (!_tools.TryGetValue(toolName, out var tool))
            {
                _logger.LogWarning("⚠️ MCP Server: Tool info requested for unknown tool '{ToolName}'", toolName);
                activity?.SetTag("result", "not_found");
                return null;
            }
            
            var toolInfo = new McpToolInfo
            {
                Name = tool.Name,
                Description = tool.Description,
                InputSchema = tool.InputSchema
            };
            
            _logger.LogDebug("ℹ️ MCP Server: Provided info for tool '{ToolName}'", toolName);
            activity?.SetTag("result", "found");
            
            return toolInfo;
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            _logger.LogError(ex, "❌ MCP Server: Failed to get tool info for '{ToolName}'", toolName);
            throw;
        }
    }

    /// <summary>
    /// Check if a tool is available
    /// </summary>
    public bool IsToolAvailable(string toolName)
    {
        var available = _tools.ContainsKey(toolName);
        _logger.LogDebug("🔍 MCP Server: Tool '{ToolName}' availability check: {Available}", toolName, available);
        return available;
    }

    /// <summary>
    /// Get server health status
    /// </summary>
    public Dictionary<string, object> GetHealthStatus()
    {
        using var activity = ActivitySource.StartActivity("MCP.GetHealthStatus");
        
        try
        {
            var healthStatus = new Dictionary<string, object>
            {
                ["status"] = "healthy",
                ["server_name"] = "chatcomplete",
                ["server_version"] = "1.0.0",
                ["tools_available"] = _tools.Count,
                ["tools"] = _tools.Keys.ToList(),
                ["uptime_seconds"] = Environment.TickCount64 / 1000,
                ["timestamp"] = DateTime.UtcNow.ToString("O")
            };
            
            _logger.LogDebug("💚 MCP Server: Health check - {ToolCount} tools available", _tools.Count);
            
            return healthStatus;
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            _logger.LogError(ex, "❌ MCP Server: Failed to get health status");
            throw;
        }
    }
}