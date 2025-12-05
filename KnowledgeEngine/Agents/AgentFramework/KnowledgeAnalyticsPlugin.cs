using System.ComponentModel;
using System.Text;
using Knowledge.Analytics.Services;
using Knowledge.Data;
using KnowledgeEngine.Agents.Models;
using KnowledgeEngine.Persistence;
using KnowledgeEngine.Persistence.VectorStores;
using Microsoft.Data.Sqlite;

namespace KnowledgeEngine.Agents.AgentFramework;

/// <summary>
/// Agent Framework plugin for providing comprehensive knowledge base analytics and summary information.
/// Migrated from Semantic Kernel's KnowledgeAnalyticsAgent.
/// </summary>
public sealed class KnowledgeAnalyticsPlugin
{
    private readonly IKnowledgeRepository _knowledgeRepository;
    private readonly IUsageTrackingService _usageTrackingService;
    private readonly ISqliteDbContext _dbContext;
    private readonly IVectorStoreStrategy _vectorStore;

    public KnowledgeAnalyticsPlugin(
        IKnowledgeRepository knowledgeRepository,
        IUsageTrackingService usageTrackingService,
        ISqliteDbContext dbContext,
        IVectorStoreStrategy vectorStore)
    {
        _knowledgeRepository = knowledgeRepository;
        _usageTrackingService = usageTrackingService;
        _dbContext = dbContext;
        _vectorStore = vectorStore;
    }

    [Description(
        "Get comprehensive summary and analytics of all knowledge bases in the system including document counts, chunk statistics, size, and activity levels."
    )]
    public async Task<string> GetKnowledgeBaseSummaryAsync(
        [Description("Include detailed metrics and statistics")] bool includeMetrics = true,
        [Description("Sort by: activity, size, age, alphabetical")] string sortBy = "activity"
    )
    {
        Console.WriteLine(
            $"📚 [AF] KnowledgeAnalyticsPlugin.GetKnowledgeBaseSummaryAsync called - includeMetrics: {includeMetrics}, sortBy: {sortBy}"
        );

        try
        {
            // Get basic knowledge base data from SQLite (active only)
            var knowledgeBases = await _knowledgeRepository.GetAllAsync();

            // Also check for collections that exist in the vector store but not in SQLite
            var vectorCollections = await _vectorStore.ListCollectionsAsync();
            var sqliteCollectionIds = knowledgeBases.Select(kb => kb.Id).ToHashSet();

            // Find orphaned collections in vector store
            var orphanedCollections = vectorCollections
                .Where(collection => !sqliteCollectionIds.Contains(collection))
                .ToList();

            if (orphanedCollections.Any())
            {
                Console.WriteLine($"⚠️ [AF] Found {orphanedCollections.Count} orphaned collections in vector store: {string.Join(", ", orphanedCollections)}");
            }

            // If no active knowledge bases found, check if there are any in vector store
            if (!knowledgeBases.Any())
            {
                if (vectorCollections.Any())
                {
                    return $"📚 Found {vectorCollections.Count} collections in vector store but none are active in the database. Check for synchronization issues.";
                }
                return "📚 No knowledge bases found. Upload some documents to create your first knowledge base.";
            }

            // Get detailed analytics for each knowledge base
            var summaries = new List<KnowledgeBaseSummary>();

            foreach (var kb in knowledgeBases)
            {
                var summary = await GetKnowledgeBaseDetailedSummaryAsync(kb.Id);
                summaries.Add(summary);
            }

            // Sort based on requested criteria
            summaries = SortKnowledgeBaseSummaries(summaries, sortBy);

            // Format response with orphaned collection warning if needed
            var response = FormatKnowledgeBaseSummaryResponse(summaries, includeMetrics, sortBy);

            if (orphanedCollections.Any())
            {
                response += $"\n\n⚠️ **System Warning**: Found {orphanedCollections.Count} orphaned collections in vector store that aren't tracked in the database: {string.Join(", ", orphanedCollections)}";
            }

            return response;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ [AF] Error in GetKnowledgeBaseSummaryAsync: {ex.Message}");
            return $"❌ Error retrieving knowledge base summary: {ex.Message}";
        }
    }

    /// <summary>
    /// Gets detailed summary information for a specific knowledge base
    /// </summary>
    private async Task<KnowledgeBaseSummary> GetKnowledgeBaseDetailedSummaryAsync(string knowledgeId)
    {
        var summary = new KnowledgeBaseSummary
        {
            KnowledgeId = knowledgeId,
            DisplayName = knowledgeId // Default to ID, will be updated below
        };

        try
        {
            // Get detailed information from SQLite database
            const string sql = """
                SELECT Name, DocumentCount, ChunkCount, TotalTokens, CreatedAt, UpdatedAt
                FROM KnowledgeCollections
                WHERE CollectionId = @collectionId AND Status = 'Active'
                """;

            var connection = await _dbContext.GetConnectionAsync();
            using var command = new SqliteCommand(sql, connection);
            command.Parameters.AddWithValue("@collectionId", knowledgeId);

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                summary.DisplayName = reader.GetString(0);
                summary.DocumentCount = reader.GetInt32(1);
                summary.ChunkCount = reader.GetInt32(2);

                // Handle TotalTokens (might be null)
                if (!reader.IsDBNull(3))
                    summary.SizeBytes = reader.GetInt64(3) * 4; // Rough estimate: 4 bytes per token

                // Handle dates
                if (!reader.IsDBNull(5))
                    summary.LastUpdated = reader.GetDateTime(5);
                else if (!reader.IsDBNull(4))
                    summary.LastUpdated = reader.GetDateTime(4);
            }

            // Get usage statistics from usage tracking service
            await EnrichWithUsageStatisticsAsync(summary);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ [AF] Error getting details for knowledge base {knowledgeId}: {ex.Message}");
        }

        return summary;
    }

    /// <summary>
    /// Enriches summary with usage statistics and activity level
    /// </summary>
    private async Task EnrichWithUsageStatisticsAsync(KnowledgeBaseSummary summary)
    {
        try
        {
            // Get usage statistics for the knowledge base
            var usageStats = await _usageTrackingService.GetKnowledgeUsageStatsAsync();
            var kbUsage = usageStats.FirstOrDefault(u => u.KnowledgeId == summary.KnowledgeId);

            if (kbUsage != null)
            {
                summary.MonthlyQueryCount = kbUsage.QueryCount;
            }

            // Determine activity level based on query count
            summary.ActivityLevel = DetermineActivityLevel(summary.MonthlyQueryCount);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ [AF] Error getting usage stats for {summary.KnowledgeId}: {ex.Message}");
            summary.ActivityLevel = "Unknown";
        }
    }

    /// <summary>
    /// Determines activity level based on monthly query count
    /// </summary>
    private static string DetermineActivityLevel(int monthlyQueryCount)
    {
        return monthlyQueryCount switch
        {
            0 => "None",
            >= 1 and < 10 => "Low",
            >= 10 and < 50 => "Medium",
            >= 50 => "High",
            _ => "Unknown"
        };
    }

    /// <summary>
    /// Sorts knowledge base summaries based on specified criteria
    /// </summary>
    private static List<KnowledgeBaseSummary> SortKnowledgeBaseSummaries(
        List<KnowledgeBaseSummary> summaries,
        string sortBy)
    {
        return sortBy.ToLowerInvariant() switch
        {
            "activity" => summaries.OrderByDescending(s => s.MonthlyQueryCount).ToList(),
            "size" => summaries.OrderByDescending(s => s.ChunkCount).ToList(),
            "age" => summaries.OrderBy(s => s.LastUpdated).ToList(),
            "alphabetical" => summaries.OrderBy(s => s.DisplayName).ToList(),
            _ => summaries.OrderByDescending(s => s.MonthlyQueryCount).ToList() // Default to activity
        };
    }

    /// <summary>
    /// Formats the knowledge base summary response for user consumption
    /// </summary>
    private static string FormatKnowledgeBaseSummaryResponse(
        List<KnowledgeBaseSummary> summaries,
        bool includeMetrics,
        string sortBy)
    {
        var response = new StringBuilder();

        // Header
        response.AppendLine($"📊 Knowledge Base Summary ({summaries.Count} total)");
        response.AppendLine();

        var sortDescription = sortBy.ToLowerInvariant() switch
        {
            "activity" => "Activity (Queries/Month)",
            "size" => "Size (Chunk Count)",
            "age" => "Age (Oldest First)",
            "alphabetical" => "Name (A-Z)",
            _ => "Activity"
        };

        response.AppendLine($"📈 **Sorted by {sortDescription}:**");

        // Knowledge base entries
        for (int i = 0; i < summaries.Count; i++)
        {
            var summary = summaries[i];
            response.AppendLine($"{i + 1}. **{summary.DisplayName}**");

            if (includeMetrics)
            {
                var metrics = new List<string>
                {
                    $"Documents: {summary.DocumentCount} files",
                    $"Chunks: {summary.ChunkCount:N0}",
                };

                if (summary.SizeBytes > 0)
                {
                    metrics.Add($"Size: {summary.FormattedSize}");
                }

                metrics.Add($"Last Updated: {summary.FormattedLastUpdated}");

                if (summary.MonthlyQueryCount > 0)
                {
                    metrics.Add($"Activity: {summary.ActivityLevel} ({summary.MonthlyQueryCount} queries this month)");
                }
                else
                {
                    metrics.Add($"Activity: {summary.ActivityLevel}");
                }

                response.AppendLine($"   - {string.Join(" | ", metrics)}");
            }

            response.AppendLine();
        }

        // Summary statistics
        if (includeMetrics && summaries.Any())
        {
            var totalDocs = summaries.Sum(s => s.DocumentCount);
            var totalChunks = summaries.Sum(s => s.ChunkCount);
            var totalQueries = summaries.Sum(s => s.MonthlyQueryCount);
            var activeKBs = summaries.Count(s => s.MonthlyQueryCount > 0);

            response.AppendLine("📈 **System Totals:**");
            response.AppendLine($"- Total Documents: {totalDocs:N0}");
            response.AppendLine($"- Total Chunks: {totalChunks:N0}");
            response.AppendLine($"- Active Knowledge Bases: {activeKBs}/{summaries.Count}");

            if (totalQueries > 0)
            {
                response.AppendLine($"- Total Monthly Queries: {totalQueries:N0}");
            }
        }

        return response.ToString().Trim();
    }
}
