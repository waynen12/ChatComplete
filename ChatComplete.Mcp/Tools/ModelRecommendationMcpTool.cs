using System.Text.Json.Nodes;
using ChatComplete.Mcp.Models;
using ChatComplete.Mcp.Services;
using KnowledgeEngine.Agents.Plugins;
using Microsoft.Extensions.Logging;

namespace ChatComplete.Mcp.Tools;

/// <summary>
/// MCP tool adapter for ModelRecommendationAgent - exposes model analytics via MCP
/// </summary>
public class ModelRecommendationMcpTool : BaseMcpToolProvider
{
    private readonly ModelRecommendationAgent _modelRecommendationAgent;
    
    public ModelRecommendationMcpTool(ModelRecommendationAgent modelRecommendationAgent, ILogger<ModelRecommendationMcpTool> logger) 
        : base(logger)
    {
        _modelRecommendationAgent = modelRecommendationAgent ?? throw new ArgumentNullException(nameof(modelRecommendationAgent));
    }

    public override string Name => "get_popular_models";

    public override string Description => 
        "Get the most popular AI models currently in use based on actual usage statistics and performance metrics. " +
        "Returns model rankings with conversation counts, success rates, response times, and intelligent recommendations.";

    public override JsonObject InputSchema => new()
    {
        ["type"] = "object",
        ["properties"] = new JsonObject
        {
            ["count"] = new JsonObject
            {
                ["type"] = "integer",
                ["description"] = "Number of top models to return",
                ["default"] = 3,
                ["minimum"] = 1,
                ["maximum"] = 10
            },
            ["period"] = new JsonObject
            {
                ["type"] = "string",
                ["description"] = "Time period filter for usage statistics",
                ["default"] = "monthly",
                ["enum"] = new JsonArray { "daily", "weekly", "monthly", "all-time" }
            },
            ["provider"] = new JsonObject
            {
                ["type"] = "string",
                ["description"] = "Filter by AI provider or show all",
                ["default"] = "all",
                ["enum"] = new JsonArray { "all", "OpenAI", "Anthropic", "Google", "Ollama" }
            }
        },
        ["required"] = new JsonArray()
    };

    protected override async Task<McpToolResult> ExecuteToolAsync(JsonObject parameters, CancellationToken cancellationToken)
    {
        try
        {
            // Extract parameters
            var count = GetIntParameter(parameters, "count", 3);
            var period = GetStringParameter(parameters, "period", "monthly");
            var provider = GetStringParameter(parameters, "provider", "all");

            // Validate parameters
            if (count < 1 || count > 10)
            {
                return McpToolResult.Error("count must be between 1 and 10");
            }

            Logger.LogInformation("🏆 ModelRecommendation MCP: Getting popular models - count: {Count}, period: {Period}, provider: {Provider}", 
                count, period, provider);

            // Call our existing ModelRecommendationAgent
            var modelsResult = await _modelRecommendationAgent.GetPopularModelsAsync(
                count, 
                period ?? "monthly", 
                provider ?? "all"
            );

            // Add MCP-specific metadata
            var metadata = new Dictionary<string, object>
            {
                ["tool_name"] = Name,
                ["count"] = count,
                ["period"] = period ?? "monthly",
                ["provider"] = provider ?? "all",
                ["timestamp"] = DateTime.UtcNow.ToString("O")
            };

            Logger.LogInformation("✅ ModelRecommendation MCP: Successfully retrieved popular models");

            return McpToolResult.Success(modelsResult, metadata);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "❌ ModelRecommendation MCP: Failed to get popular models");
            return McpToolResult.Error($"Failed to retrieve popular models: {ex.Message}", ex);
        }
    }
}

/// <summary>
/// MCP tool adapter for model performance analysis
/// </summary>
public class ModelPerformanceMcpTool : BaseMcpToolProvider
{
    private readonly ModelRecommendationAgent _modelRecommendationAgent;
    
    public ModelPerformanceMcpTool(ModelRecommendationAgent modelRecommendationAgent, ILogger<ModelPerformanceMcpTool> logger) 
        : base(logger)
    {
        _modelRecommendationAgent = modelRecommendationAgent ?? throw new ArgumentNullException(nameof(modelRecommendationAgent));
    }

    public override string Name => "analyze_model_performance";

    public override string Description => 
        "Get detailed performance analysis for a specific AI model, including success rates, response times, and usage patterns. " +
        "Provides comprehensive metrics and performance assessment for any model in the system.";

    public override JsonObject InputSchema => new()
    {
        ["type"] = "object",
        ["properties"] = new JsonObject
        {
            ["modelName"] = new JsonObject
            {
                ["type"] = "string",
                ["description"] = "Name of the model to analyze (e.g., 'gpt-4o', 'claude-sonnet-4', 'gemini-1.5-flash')"
            },
            ["provider"] = new JsonObject
            {
                ["type"] = "string",
                ["description"] = "Provider of the model (optional filter)",
                ["enum"] = new JsonArray { "OpenAI", "Anthropic", "Google", "Ollama" }
            }
        },
        ["required"] = new JsonArray { "modelName" }
    };

    protected override async Task<McpToolResult> ExecuteToolAsync(JsonObject parameters, CancellationToken cancellationToken)
    {
        try
        {
            // Extract parameters
            var modelName = GetStringParameter(parameters, "modelName");
            var provider = GetStringParameter(parameters, "provider");

            if (string.IsNullOrWhiteSpace(modelName))
            {
                return McpToolResult.Error("modelName parameter is required and cannot be empty");
            }

            Logger.LogInformation("📊 ModelPerformance MCP: Analyzing model '{ModelName}' from provider '{Provider}'", 
                modelName, provider ?? "any");

            // Call our existing ModelRecommendationAgent
            var performanceResult = await _modelRecommendationAgent.GetModelPerformanceAnalysisAsync(
                modelName, 
                provider
            );

            // Add MCP-specific metadata
            var metadata = new Dictionary<string, object>
            {
                ["tool_name"] = Name,
                ["model_name"] = modelName,
                ["provider"] = provider ?? "any",
                ["timestamp"] = DateTime.UtcNow.ToString("O")
            };

            Logger.LogInformation("✅ ModelPerformance MCP: Successfully analyzed model '{ModelName}'", modelName);

            return McpToolResult.Success(performanceResult, metadata);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "❌ ModelPerformance MCP: Failed to analyze model performance");
            return McpToolResult.Error($"Failed to analyze model performance: {ex.Message}", ex);
        }
    }
}

/// <summary>
/// MCP tool adapter for model comparison
/// </summary>
public class ModelComparisonMcpTool : BaseMcpToolProvider
{
    private readonly ModelRecommendationAgent _modelRecommendationAgent;
    
    public ModelComparisonMcpTool(ModelRecommendationAgent modelRecommendationAgent, ILogger<ModelComparisonMcpTool> logger) 
        : base(logger)
    {
        _modelRecommendationAgent = modelRecommendationAgent ?? throw new ArgumentNullException(nameof(modelRecommendationAgent));
    }

    public override string Name => "compare_models";

    public override string Description => 
        "Compare multiple AI models side by side based on performance metrics, cost efficiency, and usage patterns. " +
        "Provides detailed comparison analysis with recommendations for optimal model selection.";

    public override JsonObject InputSchema => new()
    {
        ["type"] = "object",
        ["properties"] = new JsonObject
        {
            ["modelNames"] = new JsonObject
            {
                ["type"] = "string",
                ["description"] = "Comma-separated list of model names to compare (e.g., 'gpt-4o,claude-sonnet-4,gemini-1.5-flash')"
            },
            ["focus"] = new JsonObject
            {
                ["type"] = "string",
                ["description"] = "Comparison focus area",
                ["default"] = "all",
                ["enum"] = new JsonArray { "performance", "usage", "efficiency", "all" }
            }
        },
        ["required"] = new JsonArray { "modelNames" }
    };

    protected override async Task<McpToolResult> ExecuteToolAsync(JsonObject parameters, CancellationToken cancellationToken)
    {
        try
        {
            // Extract parameters
            var modelNames = GetStringParameter(parameters, "modelNames");
            var focus = GetStringParameter(parameters, "focus", "all");

            if (string.IsNullOrWhiteSpace(modelNames))
            {
                return McpToolResult.Error("modelNames parameter is required and cannot be empty");
            }

            // Validate that we have at least 2 models to compare
            var models = modelNames.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                  .Select(m => m.Trim())
                                  .ToList();
                                  
            if (models.Count < 2)
            {
                return McpToolResult.Error("At least 2 model names are required for comparison");
            }

            Logger.LogInformation("⚖️ ModelComparison MCP: Comparing {Count} models with focus '{Focus}' - models: {Models}", 
                models.Count, focus, string.Join(", ", models));

            // Call our existing ModelRecommendationAgent
            var comparisonResult = await _modelRecommendationAgent.CompareModelsAsync(
                modelNames, 
                focus ?? "all"
            );

            // Add MCP-specific metadata
            var metadata = new Dictionary<string, object>
            {
                ["tool_name"] = Name,
                ["model_names"] = modelNames,
                ["model_count"] = models.Count,
                ["focus"] = focus ?? "all",
                ["timestamp"] = DateTime.UtcNow.ToString("O")
            };

            Logger.LogInformation("✅ ModelComparison MCP: Successfully compared {Count} models", models.Count);

            return McpToolResult.Success(comparisonResult, metadata);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "❌ ModelComparison MCP: Failed to compare models");
            return McpToolResult.Error($"Failed to compare models: {ex.Message}", ex);
        }
    }
}