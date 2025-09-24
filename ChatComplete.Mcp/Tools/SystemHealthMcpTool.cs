using System.Text.Json.Nodes;
using ChatComplete.Mcp.Models;
using ChatComplete.Mcp.Services;
using KnowledgeEngine.Agents.Plugins;
using Microsoft.Extensions.Logging;

namespace ChatComplete.Mcp.Tools;

/// <summary>
/// MCP tool adapter for SystemHealthAgent - exposes system health monitoring via MCP
/// </summary>
public class SystemHealthMcpTool : BaseMcpToolProvider
{
    private readonly SystemHealthAgent _systemHealthAgent;
    
    public SystemHealthMcpTool(SystemHealthAgent systemHealthAgent, ILogger<SystemHealthMcpTool> logger) 
        : base(logger)
    {
        _systemHealthAgent = systemHealthAgent ?? throw new ArgumentNullException(nameof(systemHealthAgent));
    }

    public override string Name => "get_system_health";

    public override string Description => 
        "Get comprehensive system health status with component details, metrics, and intelligent recommendations. " +
        "Monitors SQLite, Qdrant, Ollama, OpenAI, Anthropic, and Google AI components with performance analytics.";

    public override JsonObject InputSchema => new()
    {
        ["type"] = "object",
        ["properties"] = new JsonObject
        {
            ["includeDetailedMetrics"] = new JsonObject
            {
                ["type"] = "boolean",
                ["description"] = "Include detailed performance metrics and component analysis",
                ["default"] = true
            },
            ["scope"] = new JsonObject
            {
                ["type"] = "string", 
                ["description"] = "Check specific component scope: all, critical-only, database, vector-store, ai-services",
                ["default"] = "all",
                ["enum"] = new JsonArray { "all", "critical-only", "database", "vector-store", "ai-services" }
            },
            ["includeRecommendations"] = new JsonObject
            {
                ["type"] = "boolean",
                ["description"] = "Include actionable recommendations for issues found", 
                ["default"] = true
            }
        },
        ["required"] = new JsonArray()
    };

    protected override async Task<McpToolResult> ExecuteToolAsync(JsonObject parameters, CancellationToken cancellationToken)
    {
        try
        {
            // Extract parameters
            var includeDetailedMetrics = GetBoolParameter(parameters, "includeDetailedMetrics", true);
            var scope = GetStringParameter(parameters, "scope", "all");
            var includeRecommendations = GetBoolParameter(parameters, "includeRecommendations", true);

            Logger.LogInformation("🏥 SystemHealth MCP: Getting system health - scope: {Scope}, metrics: {Metrics}, recommendations: {Recommendations}", 
                scope, includeDetailedMetrics, includeRecommendations);

            // Call our existing SystemHealthAgent
            var healthResult = await _systemHealthAgent.GetSystemHealthAsync(
                includeDetailedMetrics, 
                scope ?? "all", 
                includeRecommendations
            );

            // Add MCP-specific metadata
            var metadata = new Dictionary<string, object>
            {
                ["tool_name"] = Name,
                ["scope"] = scope ?? "all",
                ["detailed_metrics"] = includeDetailedMetrics,
                ["recommendations_included"] = includeRecommendations,
                ["timestamp"] = DateTime.UtcNow.ToString("O")
            };

            Logger.LogInformation("✅ SystemHealth MCP: Successfully retrieved system health status");

            return McpToolResult.Success(healthResult, metadata);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "❌ SystemHealth MCP: Failed to get system health");
            return McpToolResult.Error($"Failed to retrieve system health: {ex.Message}", ex);
        }
    }
}

/// <summary>
/// MCP tool adapter for individual component health checks
/// </summary>
public class ComponentHealthMcpTool : BaseMcpToolProvider
{
    private readonly SystemHealthAgent _systemHealthAgent;
    
    public ComponentHealthMcpTool(SystemHealthAgent systemHealthAgent, ILogger<ComponentHealthMcpTool> logger) 
        : base(logger)
    {
        _systemHealthAgent = systemHealthAgent ?? throw new ArgumentNullException(nameof(systemHealthAgent));
    }

    public override string Name => "check_component_health";

    public override string Description => 
        "Check the health status of a specific system component with detailed metrics and diagnostic information. " +
        "Available components: SQLite, Qdrant, Ollama, OpenAI, Anthropic, Google AI.";

    public override JsonObject InputSchema => new()
    {
        ["type"] = "object",
        ["properties"] = new JsonObject
        {
            ["componentName"] = new JsonObject
            {
                ["type"] = "string",
                ["description"] = "Name of the component to check (e.g., SQLite, Qdrant, Ollama, OpenAI, Anthropic, 'Google AI')",
                ["enum"] = new JsonArray { "SQLite", "Qdrant", "Ollama", "OpenAI", "Anthropic", "Google AI" }
            },
            ["includeMetrics"] = new JsonObject
            {
                ["type"] = "boolean",
                ["description"] = "Include detailed metrics and diagnostic information",
                ["default"] = true
            }
        },
        ["required"] = new JsonArray { "componentName" }
    };

    protected override async Task<McpToolResult> ExecuteToolAsync(JsonObject parameters, CancellationToken cancellationToken)
    {
        try
        {
            // Extract parameters
            var componentName = GetStringParameter(parameters, "componentName");
            var includeMetrics = GetBoolParameter(parameters, "includeMetrics", true);

            if (string.IsNullOrEmpty(componentName))
            {
                return McpToolResult.Error("componentName parameter is required");
            }

            Logger.LogInformation("🔍 ComponentHealth MCP: Checking {ComponentName} health - metrics: {Metrics}", 
                componentName, includeMetrics);

            // Call our existing SystemHealthAgent
            var healthResult = await _systemHealthAgent.CheckComponentHealthAsync(
                componentName, 
                includeMetrics
            );

            // Add MCP-specific metadata
            var metadata = new Dictionary<string, object>
            {
                ["tool_name"] = Name,
                ["component"] = componentName,
                ["metrics_included"] = includeMetrics,
                ["timestamp"] = DateTime.UtcNow.ToString("O")
            };

            Logger.LogInformation("✅ ComponentHealth MCP: Successfully checked {ComponentName} health", componentName);

            return McpToolResult.Success(healthResult, metadata);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "❌ ComponentHealth MCP: Failed to check component health");
            return McpToolResult.Error($"Failed to check component health: {ex.Message}", ex);
        }
    }
}

/// <summary>
/// MCP tool adapter for system performance metrics
/// </summary>
public class SystemMetricsMcpTool : BaseMcpToolProvider
{
    private readonly SystemHealthAgent _systemHealthAgent;
    
    public SystemMetricsMcpTool(SystemHealthAgent systemHealthAgent, ILogger<SystemMetricsMcpTool> logger) 
        : base(logger)
    {
        _systemHealthAgent = systemHealthAgent ?? throw new ArgumentNullException(nameof(systemHealthAgent));
    }

    public override string Name => "get_system_metrics";

    public override string Description => 
        "Get system performance metrics and resource utilization statistics with formatted output. " +
        "Includes performance data, resource usage, and system analytics.";

    public override JsonObject InputSchema => new()
    {
        ["type"] = "object", 
        ["properties"] = new JsonObject
        {
            ["includeFormatted"] = new JsonObject
            {
                ["type"] = "boolean",
                ["description"] = "Include formatted human-readable metrics",
                ["default"] = true
            },
            ["focus"] = new JsonObject
            {
                ["type"] = "string",
                ["description"] = "Focus area for metrics: performance, resources, usage, all",
                ["default"] = "all",
                ["enum"] = new JsonArray { "performance", "resources", "usage", "all" }
            }
        },
        ["required"] = new JsonArray()
    };

    protected override async Task<McpToolResult> ExecuteToolAsync(JsonObject parameters, CancellationToken cancellationToken)
    {
        try
        {
            // Extract parameters
            var includeFormatted = GetBoolParameter(parameters, "includeFormatted", true);
            var focus = GetStringParameter(parameters, "focus", "all");

            Logger.LogInformation("📊 SystemMetrics MCP: Getting system metrics - focus: {Focus}, formatted: {Formatted}", 
                focus, includeFormatted);

            // Call our existing SystemHealthAgent
            var metricsResult = await _systemHealthAgent.GetSystemMetricsAsync(
                includeFormatted, 
                focus ?? "all"
            );

            // Add MCP-specific metadata
            var metadata = new Dictionary<string, object>
            {
                ["tool_name"] = Name,
                ["focus"] = focus ?? "all",
                ["formatted"] = includeFormatted,
                ["timestamp"] = DateTime.UtcNow.ToString("O")
            };

            Logger.LogInformation("✅ SystemMetrics MCP: Successfully retrieved system metrics");

            return McpToolResult.Success(metricsResult, metadata);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "❌ SystemMetrics MCP: Failed to get system metrics");
            return McpToolResult.Error($"Failed to retrieve system metrics: {ex.Message}", ex);
        }
    }
}