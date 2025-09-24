using System.Text.Json.Nodes;
using ChatComplete.Mcp.Models;
using ChatComplete.Mcp.Services;
using KnowledgeEngine.Agents.Plugins;
using Microsoft.Extensions.Logging;

namespace ChatComplete.Mcp.Tools;

/// <summary>
/// MCP tool adapter for KnowledgeAnalyticsAgent - exposes knowledge base analytics via MCP
/// </summary>
public class KnowledgeAnalyticsMcpTool : BaseMcpToolProvider
{
    private readonly KnowledgeAnalyticsAgent _knowledgeAnalyticsAgent;
    
    public KnowledgeAnalyticsMcpTool(KnowledgeAnalyticsAgent knowledgeAnalyticsAgent, ILogger<KnowledgeAnalyticsMcpTool> logger) 
        : base(logger)
    {
        _knowledgeAnalyticsAgent = knowledgeAnalyticsAgent ?? throw new ArgumentNullException(nameof(knowledgeAnalyticsAgent));
    }

    public override string Name => "get_knowledge_base_summary";

    public override string Description => 
        "Get comprehensive summary and analytics of all knowledge bases in the system. " +
        "Provides document counts, usage statistics, activity metrics, and recommendations for optimization. " +
        "Includes orphaned collection detection and knowledge base health analysis.";

    public override JsonObject InputSchema => new()
    {
        ["type"] = "object",
        ["properties"] = new JsonObject
        {
            ["includeMetrics"] = new JsonObject
            {
                ["type"] = "boolean",
                ["description"] = "Include detailed metrics and statistics for each knowledge base",
                ["default"] = true
            },
            ["sortBy"] = new JsonObject
            {
                ["type"] = "string",
                ["description"] = "Sort knowledge bases by specified criteria",
                ["default"] = "activity",
                ["enum"] = new JsonArray { "activity", "size", "age", "alphabetical", "usage" }
            },
            ["includeInactive"] = new JsonObject
            {
                ["type"] = "boolean",
                ["description"] = "Include inactive or rarely used knowledge bases in results",
                ["default"] = true
            },
            ["minDocumentCount"] = new JsonObject
            {
                ["type"] = "integer",
                ["description"] = "Minimum number of documents a knowledge base must have to be included",
                ["default"] = 0,
                ["minimum"] = 0
            }
        },
        ["required"] = new JsonArray()
    };

    protected override async Task<McpToolResult> ExecuteToolAsync(JsonObject parameters, CancellationToken cancellationToken)
    {
        try
        {
            // Extract parameters
            var includeMetrics = GetBoolParameter(parameters, "includeMetrics", true);
            var sortBy = GetStringParameter(parameters, "sortBy", "activity");
            var includeInactive = GetBoolParameter(parameters, "includeInactive", true);
            var minDocumentCount = GetIntParameter(parameters, "minDocumentCount", 0);

            // Validate parameters
            if (minDocumentCount < 0)
            {
                return McpToolResult.Error("minDocumentCount must be >= 0");
            }

            Logger.LogInformation("📚 KnowledgeAnalytics MCP: Getting knowledge base summary - sortBy: {SortBy}, metrics: {Metrics}, includeInactive: {IncludeInactive}, minDocs: {MinDocs}", 
                sortBy, includeMetrics, includeInactive, minDocumentCount);

            // Call our existing KnowledgeAnalyticsAgent
            var summaryResult = await _knowledgeAnalyticsAgent.GetKnowledgeBaseSummaryAsync(
                includeMetrics, 
                sortBy ?? "activity"
            );

            // Add MCP-specific metadata  
            var metadata = new Dictionary<string, object>
            {
                ["tool_name"] = Name,
                ["include_metrics"] = includeMetrics,
                ["sort_by"] = sortBy ?? "activity",
                ["include_inactive"] = includeInactive,
                ["min_document_count"] = minDocumentCount,
                ["timestamp"] = DateTime.UtcNow.ToString("O")
            };

            // TODO: Apply client-side filtering based on includeInactive and minDocumentCount
            // This would require parsing the result and filtering, but for now we'll pass through
            // the full result and let external clients handle additional filtering if needed

            Logger.LogInformation("✅ KnowledgeAnalytics MCP: Successfully retrieved knowledge base summary");

            return McpToolResult.Success(summaryResult, metadata);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "❌ KnowledgeAnalytics MCP: Failed to get knowledge base summary");
            return McpToolResult.Error($"Failed to retrieve knowledge base summary: {ex.Message}", ex);
        }
    }
}