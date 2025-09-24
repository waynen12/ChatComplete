using System.Text.Json.Nodes;
using ChatComplete.Mcp.Models;
using ChatComplete.Mcp.Services;
using KnowledgeEngine.Agents.Plugins;
using Microsoft.Extensions.Logging;

namespace ChatComplete.Mcp.Tools;

/// <summary>
/// MCP tool adapter for CrossKnowledgeSearchPlugin - exposes cross-knowledge search via MCP
/// </summary>
public class CrossKnowledgeSearchMcpTool : BaseMcpToolProvider
{
    private readonly CrossKnowledgeSearchPlugin _crossKnowledgeSearchPlugin;
    
    public CrossKnowledgeSearchMcpTool(CrossKnowledgeSearchPlugin crossKnowledgeSearchPlugin, ILogger<CrossKnowledgeSearchMcpTool> logger) 
        : base(logger)
    {
        _crossKnowledgeSearchPlugin = crossKnowledgeSearchPlugin ?? throw new ArgumentNullException(nameof(crossKnowledgeSearchPlugin));
    }

    public override string Name => "search_all_knowledge_bases";

    public override string Description => 
        "Search across ALL knowledge bases to find information from uploaded documents. " +
        "Performs concurrent search with relevance scoring, result aggregation, and source attribution. " +
        "Use this when users need to find information that might be in any knowledge base.";

    public override JsonObject InputSchema => new()
    {
        ["type"] = "object",
        ["properties"] = new JsonObject
        {
            ["query"] = new JsonObject
            {
                ["type"] = "string",
                ["description"] = "The search query or question to find relevant information"
            },
            ["limit"] = new JsonObject
            {
                ["type"] = "integer",
                ["description"] = "Maximum number of results to return from each knowledge base",
                ["default"] = 5,
                ["minimum"] = 1,
                ["maximum"] = 20
            },
            ["minRelevance"] = new JsonObject
            {
                ["type"] = "number",
                ["description"] = "Minimum relevance score threshold (0.0 to 1.0)",
                ["default"] = 0.6,
                ["minimum"] = 0.0,
                ["maximum"] = 1.0
            }
        },
        ["required"] = new JsonArray { "query" }
    };

    protected override async Task<McpToolResult> ExecuteToolAsync(JsonObject parameters, CancellationToken cancellationToken)
    {
        try
        {
            // Extract parameters
            var query = GetStringParameter(parameters, "query");
            var limit = GetIntParameter(parameters, "limit", 5);
            var minRelevance = GetDoubleParameter(parameters, "minRelevance", 0.6);

            if (string.IsNullOrWhiteSpace(query))
            {
                return McpToolResult.Error("query parameter is required and cannot be empty");
            }

            // Validate parameters
            if (limit < 1 || limit > 20)
            {
                return McpToolResult.Error("limit must be between 1 and 20");
            }

            if (minRelevance < 0.0 || minRelevance > 1.0)
            {
                return McpToolResult.Error("minRelevance must be between 0.0 and 1.0");
            }

            Logger.LogInformation("🔍 CrossKnowledgeSearch MCP: Searching all knowledge bases - query: '{Query}', limit: {Limit}, minRelevance: {MinRelevance}", 
                query, limit, minRelevance);

            // Call our existing CrossKnowledgeSearchPlugin
            var searchResult = await _crossKnowledgeSearchPlugin.SearchAllKnowledgeBasesAsync(
                query, 
                limit, 
                minRelevance
            );

            // Add MCP-specific metadata
            var metadata = new Dictionary<string, object>
            {
                ["tool_name"] = Name,
                ["query"] = query,
                ["limit"] = limit,
                ["min_relevance"] = minRelevance,
                ["timestamp"] = DateTime.UtcNow.ToString("O"),
                ["search_type"] = "cross_knowledge_base"
            };

            Logger.LogInformation("✅ CrossKnowledgeSearch MCP: Successfully completed search for '{Query}'", query);

            return McpToolResult.Success(searchResult, metadata);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "❌ CrossKnowledgeSearch MCP: Failed to search knowledge bases");
            return McpToolResult.Error($"Failed to search knowledge bases: {ex.Message}", ex);
        }
    }
}