using System.ComponentModel;
using System.Text;
using ChatCompletion.Config;
using KnowledgeEngine.Models;

namespace KnowledgeEngine.Agents.AgentFramework;

/// <summary>
/// Agent Framework version of CrossKnowledgeSearchPlugin.
/// Provides cross-knowledge base search functionality for AI agents.
/// </summary>
public sealed class CrossKnowledgeSearchPlugin
{
    private readonly KnowledgeManager _knowledgeManager;
    private readonly ChatCompleteSettings _settings;

    public CrossKnowledgeSearchPlugin(KnowledgeManager knowledgeManager, ChatCompleteSettings settings)
    {
        _knowledgeManager = knowledgeManager;
        _settings = settings;
    }

    [Description(
        "Search across ALL knowledge bases to find information from uploaded documents and files. Use this ONLY when the user asks about specific technical content, documentation, or information that would be stored in uploaded documents. DO NOT use for system health checks, component status, system metrics, model recommendations, usage statistics, or system monitoring. For system health queries, use SystemHealth functions instead."
    )]
    public async Task<string> SearchAllKnowledgeBasesAsync(
        [Description("The search query or question")] string query,
        [Description("Maximum results per knowledge base")] int limit = 5,
        [Description("Minimum relevance score (0.0-1.0), use -1 for provider default")] double minRelevance = -1.0
    )
    {
        Console.WriteLine($"🔍 [AF] CrossKnowledgeSearchPlugin.SearchAllKnowledgeBasesAsync called with query: '{query}'");

        // Use configured MinRelevanceScore from active embedding provider if -1 specified
        var effectiveMinRelevance = minRelevance < 0
            ? _settings.EmbeddingProviders.GetActiveProvider().MinRelevanceScore
            : minRelevance;
        Console.WriteLine($"📊 [AF] Using minRelevance: {effectiveMinRelevance} (from {(minRelevance < 0 ? "config" : "parameter")})");

        try
        {
            var collections = await _knowledgeManager.GetAvailableCollectionsAsync();

            if (!collections.Any())
            {
                return "No knowledge bases are currently available.";
            }

            var allResults = new List<(string Collection, KnowledgeSearchResult Result)>();
            var searchTasks =
                new List<Task<(string Collection, List<KnowledgeSearchResult> Results)>>();

            foreach (var collection in collections)
            {
                searchTasks.Add(SearchCollectionAsync(collection, query, limit, effectiveMinRelevance));
            }

            var searchResults = await Task.WhenAll(searchTasks);

            foreach (var (collection, results) in searchResults)
            {
                foreach (var result in results)
                {
                    allResults.Add((collection, result));
                }
            }

            if (!allResults.Any())
            {
                return $"No relevant information found across {collections.Count} knowledge bases for query: '{query}'";
            }

            var topResults = allResults
                .OrderByDescending(item => item.Result.Score)
                .Take(limit * 2)
                .ToList();

            var response = new StringBuilder();
            response.AppendLine(
                $"Found {topResults.Count} relevant results across {collections.Count} knowledge bases:"
            );
            response.AppendLine();

            var groupedResults = topResults
                .GroupBy(item => item.Collection)
                .OrderByDescending(g => g.Max(item => item.Result.Score));

            foreach (var group in groupedResults)
            {
                response.AppendLine($"**From {group.Key}:**");
                foreach (var item in group.Take(3))
                {
                    var text = item.Result.Text;
                    var preview = text.Length > 200 ? text.Substring(0, 200) + "..." : text;
                    response.AppendLine($"- {preview}");
                    response.AppendLine($"  (Score: {item.Result.Score:F3})");
                }
                response.AppendLine();
            }

            return response.ToString();
        }
        catch (Exception ex)
        {
            return $"Error searching knowledge bases: {ex.Message}";
        }
    }

    private async Task<(
        string Collection,
        List<KnowledgeSearchResult> Results
    )> SearchCollectionAsync(string collection, string query, int limit, double minRelevance)
    {
        try
        {
            var results = await _knowledgeManager.SearchAsync(
                collection,
                query,
                limit,
                minRelevance
            );
            return (collection, results);
        }
        catch (Exception)
        {
            return (collection, new List<KnowledgeSearchResult>());
        }
    }
}
