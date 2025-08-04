using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.AI;
using KnowledgeEngine.Models;

namespace KnowledgeEngine.Persistence.VectorStores;

/// <summary>
/// Strategy interface for vector store operations across different providers.
/// Abstracts the core vector operations: upsert and search.
/// </summary>
public interface IVectorStoreStrategy
{
    /// <summary>
    /// Upserts a chunk into the vector store with metadata.
    /// Matches the signature from KnowledgeManager.UpsertAsync()
    /// </summary>
    /// <param name="collectionName">The collection/index name</param>
    /// <param name="key">Unique identifier for the chunk</param>
    /// <param name="text">The text content of the chunk</param>
    /// <param name="embedding">The vector embedding for the text</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the async operation</returns>
    Task UpsertAsync(
        string collectionName,
        string key,
        string text,
        Embedding<float> embedding,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs semantic search in the vector store.
    /// Matches the signature from KnowledgeManager.SearchAsync()
    /// </summary>
    /// <param name="collectionName">The collection/index to search</param>
    /// <param name="query">The search query text</param>
    /// <param name="embedding">The query embedding</param>
    /// <param name="limit">Maximum number of results to return</param>
    /// <param name="minRelevanceScore">Minimum similarity score threshold</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of search results ordered by relevance score</returns>
    Task<List<KnowledgeSearchResult>> SearchAsync(
        string collectionName,
        string query,
        Embedding<float> embedding,
        int limit = 10,
        double minRelevanceScore = 0.6,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists all available collections in the vector store.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of collection names</returns>
    Task<List<string>> ListCollectionsAsync(CancellationToken cancellationToken = default);
}