using System.ComponentModel;
using System.Text.Json;
using Knowledge.Analytics.Services;
using Knowledge.Data;
using KnowledgeEngine.Agents.Plugins;
using KnowledgeEngine.Persistence;
using KnowledgeEngine.Persistence.VectorStores;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace Knowledge.Mcp.Tools;

/// <summary>
/// Knowledge Analytics MCP Tool - Provides external access to knowledge base management and analytics
///
/// This tool wraps the existing KnowledgeAnalyticsAgent functionality to make knowledge base insights
/// accessible to external MCP clients for monitoring, management, and optimization purposes.
///
/// Key Features:
/// - Comprehensive knowledge base summaries and statistics
/// - Document counts, chunk statistics, and storage metrics
/// - Activity levels and usage patterns
/// - Orphaned collection detection and synchronization status
/// - Sorting and filtering options for different management needs
///
/// Dependencies:
/// - KnowledgeAnalyticsAgent: Existing analytics logic implementation
/// - IKnowledgeRepository: Knowledge metadata from SQLite database
/// - IUsageTrackingService: Usage statistics and activity tracking
/// - ISqliteDbContext: Direct database access for detailed metrics
/// - IVectorStoreStrategy: Vector store collection verification
///
/// Usage Examples:
/// - Monitoring: Track knowledge base health and usage
/// - Management: Identify collections needing attention
/// - Optimization: Find storage optimization opportunities
/// - Maintenance: Detect synchronization issues between databases
/// </summary>
[McpServerToolType]
public sealed class KnowledgeAnalyticsMcpTool
{
    /// <summary>
    /// Get comprehensive summary and analytics of all knowledge bases in the system.
    ///
    /// This method provides a complete overview of the knowledge management system including:
    /// - Active knowledge bases with document and chunk counts
    /// - Storage utilization and size metrics
    /// - Activity levels and last usage timestamps
    /// - Orphaned collection detection in vector store
    /// - Sorting options for different management perspectives
    ///
    /// Management Insights:
    /// - High-activity collections that drive value
    /// - Unused or stale collections consuming resources
    /// - Collections with sync issues between SQLite and vector store
    /// - Storage optimization opportunities
    ///
    /// Sorting Options:
    /// - activity: Most recently used collections first (default)
    /// - size: Largest collections first (storage management)
    /// - age: Oldest collections first (cleanup candidates)
    /// - alphabetical: A-Z sorting (organized view)
    ///
    /// Use Cases:
    /// - System health monitoring and dashboards
    /// - Storage capacity planning and optimization
    /// - Content management and cleanup decisions
    /// - Knowledge base lifecycle management
    /// </summary>
    /// <param name="includeMetrics">Include detailed metrics and statistics for each knowledge base</param>
    /// <param name="sortBy">Sort order: activity, size, age, or alphabetical</param>
    /// <param name="serviceProvider">Service provider for dependency injection</param>
    /// <returns>Comprehensive knowledge base summary with analytics</returns>
    [McpServerTool]
    [Description(
        "Get comprehensive summary and analytics of all knowledge bases including document counts, usage statistics, and storage metrics"
    )]
    public static async Task<string> GetKnowledgeBaseSummaryAsync(
        [Description("Service provider for dependency injection")] IServiceProvider serviceProvider,
        [Description(
            "Include detailed metrics and statistics for each knowledge base (default: true)"
        )]
            bool includeMetrics = true,
        [Description("Sort by: activity, size, age, or alphabetical (default: activity)")]
            string sortBy = "activity"
    )
    {
        try
        {
            // Validate parameters
            var validSortOptions = new[] { "activity", "size", "age", "alphabetical" };
            if (!validSortOptions.Contains(sortBy.ToLowerInvariant()))
            {
                return CreateErrorResponse(
                    $"SortBy must be one of: {string.Join(", ", validSortOptions)}",
                    nameof(GetKnowledgeBaseSummaryAsync)
                );
            }

            // Resolve required services from DI container
            var knowledgeRepository = serviceProvider.GetRequiredService<IKnowledgeRepository>();
            var usageTrackingService = serviceProvider.GetRequiredService<IUsageTrackingService>();
            var dbContext = serviceProvider.GetRequiredService<ISqliteDbContext>();
            var vectorStore = serviceProvider.GetRequiredService<IVectorStoreStrategy>();

            // Create knowledge analytics agent with all required dependencies
            var analyticsAgent = new KnowledgeAnalyticsAgent(
                knowledgeRepository,
                usageTrackingService,
                dbContext,
                vectorStore
            );

            // Execute comprehensive knowledge base analysis
            var results = await analyticsAgent.GetKnowledgeBaseSummaryAsync(includeMetrics, sortBy);

            Console.WriteLine(
                $"📚 MCP KnowledgeAnalytics completed - Metrics: {includeMetrics}, Sort: {sortBy}"
            );
            return results;
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("service"))
        {
            // Handle service resolution failures - typically configuration issues
            return CreateErrorResponse(
                $"Service configuration error: {ex.Message}",
                nameof(GetKnowledgeBaseSummaryAsync)
            );
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ MCP KnowledgeAnalytics error: {ex.Message}");
            return CreateErrorResponse(
                $"Failed to get knowledge base summary: {ex.Message}",
                nameof(GetKnowledgeBaseSummaryAsync)
            );
        }
    }

    /// <summary>
    /// Get knowledge base health status and synchronization analysis (Future Enhancement).
    ///
    /// This method would provide specialized health checks for knowledge base integrity:
    /// - SQLite vs Vector Store synchronization status
    /// - Missing collections or orphaned data
    /// - Index health and performance metrics
    /// - Storage consistency verification
    ///
    /// Currently planned for future implementation as a specialized health check tool.
    /// </summary>
    /// <param name="checkSynchronization">Verify sync between SQLite and vector store</param>
    /// <param name="includePerformanceMetrics">Include search performance and index health</param>
    /// <param name="serviceProvider">Service provider for dependency injection</param>
    /// <returns>Knowledge base health and synchronization status</returns>
    [McpServerTool]
    [Description(
        "Get knowledge base health status and synchronization analysis between SQLite and vector store"
    )]
    public static async Task<string> GetKnowledgeBaseHealthAsync(
        [Description("Service provider for dependency injection")] IServiceProvider serviceProvider,
        [Description("Verify synchronization between SQLite and vector store (default: true)")]
            bool checkSynchronization = true,
        [Description("Include search performance and index health metrics (default: false)")]
            bool includePerformanceMetrics = false
    )
    {
        try
        {
            // This is a planned enhancement that would provide specialized health checking
            // Currently, health information is included in the main GetKnowledgeBaseSummaryAsync method

            // For now, delegate to the main summary method with focus on synchronization issues
            var summaryResult = await GetKnowledgeBaseSummaryAsync(
                serviceProvider,
                true,
                "activity"
            );

            // Extract health-related information from the summary
            var healthReport = new
            {
                Status = "Partial Implementation",
                Message = "Knowledge base health checking is currently integrated into GetKnowledgeBaseSummaryAsync method",
                Recommendation = "Use GetKnowledgeBaseSummaryAsync for orphaned collection detection and basic health metrics",
                PlannedFeatures = new[]
                {
                    "Dedicated synchronization verification",
                    "Index performance analysis",
                    "Storage consistency checks",
                    "Automated repair suggestions",
                },
                CurrentHealthIndicators = new
                {
                    OrphanedCollectionDetection = "Available in GetKnowledgeBaseSummaryAsync",
                    BasicMetrics = "Document counts, storage usage, activity levels",
                    SynchronizationStatus = "Basic comparison between SQLite and vector store",
                },
            };

            Console.WriteLine(
                "📊 MCP KnowledgeBaseHealth - Using summary method for health insights"
            );

            return JsonSerializer.Serialize(
                healthReport,
                new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                }
            );
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ MCP KnowledgeBaseHealth error: {ex.Message}");
            return CreateErrorResponse(
                $"Failed to get knowledge base health: {ex.Message}",
                nameof(GetKnowledgeBaseHealthAsync)
            );
        }
    }

    /// <summary>
    /// Get storage optimization recommendations (Future Enhancement).
    ///
    /// This method would analyze storage usage patterns and provide actionable recommendations:
    /// - Collections with low usage that could be archived
    /// - Duplicate content detection across collections
    /// - Storage consolidation opportunities
    /// - Cleanup recommendations for stale data
    ///
    /// Currently planned for future implementation as a storage management tool.
    /// </summary>
    /// <param name="minUsageThreshold">Minimum usage threshold for active collections</param>
    /// <param name="includeCleanupSuggestions">Include specific cleanup recommendations</param>
    /// <param name="serviceProvider">Service provider for dependency injection</param>
    /// <returns>Storage optimization recommendations and cleanup suggestions</returns>
    [McpServerTool]
    [Description(
        "Get storage optimization recommendations and cleanup suggestions for knowledge bases"
    )]
    public static async Task<string> GetStorageOptimizationRecommendationsAsync(
        [Description("Service provider for dependency injection")] IServiceProvider serviceProvider,
        [Description(
            "Minimum usage threshold for considering collections active (default: 30 days)"
        )]
            int minUsageThreshold = 30,
        [Description(
            "Include specific cleanup recommendations and automation suggestions (default: true)"
        )]
            bool includeCleanupSuggestions = true
    )
    {
        try
        {
            // This is a planned enhancement for storage management
            // Currently, basic recommendations are available through the main summary method

            var optimizationReport = new
            {
                Status = "Future Enhancement",
                Message = "Storage optimization is planned for future implementation",
                CurrentCapabilities = new
                {
                    BasicAnalytics = "Available in GetKnowledgeBaseSummaryAsync",
                    ActivitySorting = "Sort by activity to identify unused collections",
                    SizeSorting = "Sort by size to identify storage-heavy collections",
                },
                PlannedFeatures = new[]
                {
                    "Automated cleanup recommendations based on usage patterns",
                    "Duplicate content detection across collections",
                    "Storage consolidation opportunities",
                    "Archive suggestions for inactive collections",
                    "Cost analysis and storage projections",
                },
                WorkaroundSuggestions = new[]
                {
                    "Use GetKnowledgeBaseSummaryAsync with sortBy='activity' to find unused collections",
                    "Use GetKnowledgeBaseSummaryAsync with sortBy='size' to find large collections",
                    "Monitor collections with last usage > 30 days for cleanup candidates",
                },
            };

            Console.WriteLine(
                "🗄️ MCP StorageOptimization - Planned feature, providing workaround guidance"
            );

            return JsonSerializer.Serialize(
                optimizationReport,
                new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                }
            );
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ MCP StorageOptimization error: {ex.Message}");
            return CreateErrorResponse(
                $"Failed to get storage optimization recommendations: {ex.Message}",
                nameof(GetStorageOptimizationRecommendationsAsync)
            );
        }
    }

    /// <summary>
    /// Creates structured error responses for MCP clients with helpful guidance.
    /// </summary>
    /// <param name="errorMessage">Descriptive error message</param>
    /// <param name="toolName">Name of the tool that encountered the error</param>
    /// <returns>JSON-formatted error response</returns>
    private static string CreateErrorResponse(string errorMessage, string toolName)
    {
        var errorResponse = new
        {
            Error = true,
            Tool = toolName,
            Message = errorMessage,
            Timestamp = DateTime.UtcNow,
            Suggestions = new[]
            {
                "Check if knowledge base system is properly configured",
                "Verify SQLite database is accessible",
                "Ensure vector store (Qdrant) is running and accessible",
                "Check MCP server logs for detailed error information",
                "Try again with different parameters if configuration is correct",
            },
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
}
