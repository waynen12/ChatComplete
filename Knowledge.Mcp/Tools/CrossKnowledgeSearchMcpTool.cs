using System.ComponentModel;
using System.Text.Json;
using KnowledgeEngine;
using KnowledgeEngine.Agents.Plugins;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol;
using ModelContextProtocol.Server;
using Knowledge.Mcp.Configuration;
using Knowledge.Mcp.Constants;

namespace Knowledge.Mcp.Tools;

/// <summary>
/// Cross-Knowledge Search MCP Tool - Provides external access to knowledge base search via Model Context Protocol
///
/// This tool wraps the existing CrossKnowledgeSearchPlugin functionality to make it accessible
/// to external MCP clients like VS Code, Claude Desktop, monitoring scripts, etc.
///
/// Key Features:
/// - Search across ALL knowledge bases simultaneously
/// - Parallel collection searching with relevance scoring
/// - Configurable result limits and relevance thresholds
/// - Structured JSON error handling for external clients
/// - Integration with existing KnowledgeEngine infrastructure
///
/// Dependencies:
/// - KnowledgeManager: Core search orchestration
/// - CrossKnowledgeSearchPlugin: Existing search logic implementation
/// - IVectorStoreStrategy: Vector store operations (Qdrant/MongoDB)
/// - IKnowledgeRepository: Knowledge metadata access
/// - IEmbeddingGenerator: Text-to-vector conversion for search queries
///
/// Usage Examples:
/// - VS Code: Search documentation while coding
/// - CLI Scripts: Automated knowledge retrieval
/// - Monitoring: Verify knowledge base content and availability
/// - External Apps: Integrate ChatComplete knowledge into other systems
/// </summary>
[McpServerToolType]
public sealed class CrossKnowledgeSearchMcpTool
{
    /// <summary>
    /// Search across ALL knowledge bases to find information from uploaded documents and files.
    ///
    /// This is the primary MCP entry point for external knowledge search requests.
    /// It leverages the existing CrossKnowledgeSearchPlugin implementation to provide
    /// consistent search behavior between internal agent mode and external MCP access.
    ///
    /// Search Process:
    /// 1. Resolve KnowledgeManager from DI container
    /// 2. Get list of available knowledge collections
    /// 3. Execute parallel searches across all collections
    /// 4. Aggregate and rank results by relevance score
    /// 5. Format results for external client consumption
    ///
    /// Performance Considerations:
    /// - Parallel collection searching for speed
    /// - Configurable limits to prevent resource exhaustion
    /// - Relevance filtering to improve result quality
    /// - Error isolation per collection (failures don't break entire search)
    ///
    /// Error Handling:
    /// - Service resolution failures → structured JSON error
    /// - Individual collection failures → graceful degradation
    /// - Invalid parameters → validation error messages
    /// - Timeout scenarios → partial results with warning
    /// </summary>
    /// <param name="query">The search query or question. Should be specific and descriptive for best results.</param>
    /// <param name="limit">Maximum results per knowledge base. Higher values increase comprehensiveness but may impact performance.</param>
    /// <param name="minRelevance">Minimum relevance score (0.0-1.0). Higher values improve precision but may miss relevant content.</param>
    /// <param name="serviceProvider">Service provider for dependency injection. Automatically provided by MCP framework.</param>
    /// <returns>Formatted search results or structured error information</returns>
    [McpServerTool]
    [Description(
        "Search across ALL knowledge bases to find information from uploaded documents and files. Use for technical documentation, code examples, configuration guides, and any content stored in the knowledge system."
    )]
    public static async Task<string> SearchAllKnowledgeBasesAsync(
        [Description("The search query or question - be specific for best results")] string query,
        [Description("Service provider for dependency injection")] IServiceProvider serviceProvider,
        [Description("Maximum results per knowledge base (configurable via McpServerSettings)")]
            int? limit = null,
        [Description(
            "Minimum relevance score 0.0-1.0 (configurable via McpServerSettings)"
        )]
            double? minRelevance = null
    )
    {
        try
        {
            // Get configuration settings
            var settings = serviceProvider.GetRequiredService<McpServerSettings>();
            
            // Apply defaults from configuration if not provided
            var actualLimit = limit ?? settings.Search.DefaultResultLimit;
            var actualMinRelevance = minRelevance ?? settings.Search.DefaultMinRelevance;
            
            // Validate input parameters using configuration values
            if (string.IsNullOrWhiteSpace(query))
            {
                return CreateErrorResponse(
                    "Query parameter is required and cannot be empty",
                    query
                );
            }

            if (actualLimit < McpToolConstants.Search.MinResultLimit || actualLimit > settings.Search.MaxResultLimit)
            {
                return CreateErrorResponse(
                    $"Limit must be between {McpToolConstants.Search.MinResultLimit} and {settings.Search.MaxResultLimit}",
                    query,
                    actualLimit.ToString()
                );
            }

            if (actualMinRelevance < McpToolConstants.Search.MinRelevanceScore || actualMinRelevance > McpToolConstants.Search.MaxRelevanceScore)
            {
                return CreateErrorResponse(
                    $"MinRelevance must be between {McpToolConstants.Search.MinRelevanceScore} and {McpToolConstants.Search.MaxRelevanceScore}",
                    query,
                    actualMinRelevance.ToString()
                );
            }

            // Resolve required services from DI container
            // This ensures we use the same configured services as the main application
            var knowledgeManager = serviceProvider.GetRequiredService<KnowledgeManager>();
            var chatSettings = serviceProvider.GetRequiredService<ChatCompletion.Config.ChatCompleteSettings>();

            // Create instance of existing search plugin to leverage proven search logic
            // This maintains consistency between internal agent mode and external MCP access
            var searchPlugin = new CrossKnowledgeSearchPlugin(knowledgeManager, chatSettings);

            // Execute search using existing implementation
            // The CrossKnowledgeSearchPlugin already handles:
            // - Parallel collection searching
            // - Result aggregation and ranking
            // - Error handling for individual collection failures
            // - Formatted output suitable for both human and programmatic consumption
            var searchResults = await searchPlugin.SearchAllKnowledgeBasesAsync(
                query,
                actualLimit,
                actualMinRelevance
            );

            // Log successful search for monitoring and debugging
            Console.WriteLine(
                $"🔍 MCP CrossKnowledgeSearch completed - Query: '{query}', Results returned"
            );

            return searchResults;
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("service"))
        {
            // Handle service resolution failures - typically configuration issues
            return CreateErrorResponse(
                "Service configuration error - check MCP server setup",
                query,
                ex.Message
            );
        }
        catch (TimeoutException ex)
        {
            // Handle search timeouts - vector store or embedding service issues
            return CreateErrorResponse(
                "Search operation timed out - check system performance",
                query,
                ex.Message
            );
        }
        catch (Exception ex)
        {
            // Handle all other exceptions with full error details for debugging
            Console.WriteLine($"❌ MCP CrossKnowledgeSearch error: {ex.Message}");
            return CreateErrorResponse("Unexpected search error occurred", query, ex.Message);
        }
    }

    /// <summary>
    /// Creates a structured error response for MCP clients.
    ///
    /// This provides consistent error formatting that external clients can reliably parse
    /// and handle programmatically. The JSON structure includes:
    /// - Clear error classification
    /// - Original query for context
    /// - Detailed error message for debugging
    /// - Timestamp for logging and correlation
    /// - Suggestions for resolution where applicable
    /// </summary>
    /// <param name="errorType">High-level error classification</param>
    /// <param name="query">Original query that caused the error</param>
    /// <param name="details">Detailed error information</param>
    /// <returns>JSON-formatted error response</returns>
    private static string CreateErrorResponse(
        string errorType,
        string query,
        string? details = null
    )
    {
        var errorResponse = new
        {
            Error = true,
            ErrorType = errorType,
            Message = details ?? errorType,
            Query = query,
            Timestamp = DateTime.UtcNow,
            Suggestions = GetErrorSuggestions(errorType),
        };

        return JsonSerializer.Serialize(
            errorResponse,
            new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            }
        );
    }

    /// <summary>
    /// Provides contextual suggestions based on error type to help users resolve issues.
    /// </summary>
    /// <param name="errorType">The type of error that occurred</param>
    /// <returns>Array of suggested resolution steps</returns>
    private static string[] GetErrorSuggestions(string errorType)
    {
        return errorType.ToLowerInvariant() switch
        {
            var e when e.Contains("query") => new[]
            {
                "Provide a non-empty search query",
                "Use specific keywords for better results",
                "Try different search terms if no results found",
            },
            var e when e.Contains("limit") => new[]
            {
                "Use limit values within configured range",
                "Start with smaller limits for faster results",
                "Check McpServerSettings.Search.MaxResultLimit for maximum allowed value",
            },
            var e when e.Contains("relevance") => new[]
            {
                "Use relevance scores between 0.0 and 1.0",
                "Lower values include more results with reduced precision",
                "Higher values provide more precise matches with fewer results",
                "Check McpServerSettings.Search.DefaultMinRelevance for recommended value",
            },
            var e when e.Contains("service") => McpToolConstants.ErrorMessages.ConfigurationSuggestions,
            var e when e.Contains("timeout") => new[]
            {
                "Check if vector store (Qdrant) is accessible",
                "Verify embedding service is responding",
                "Try a simpler query or reduce the limit parameter",
            },
            _ => McpToolConstants.ErrorMessages.CommonSuggestions,
        };
    }
}
