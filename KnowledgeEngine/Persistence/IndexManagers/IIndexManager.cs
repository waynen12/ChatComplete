using System.Threading;
using System.Threading.Tasks;

namespace KnowledgeEngine.Persistence.IndexManagers;

/// <summary>
/// Abstraction for managing search indexes across different vector store providers.
/// Supports both MongoDB Atlas Vector Search indexes and Qdrant collections.
/// </summary>
public interface IIndexManager
{
    /// <summary>
    /// Creates a search index for the specified collection if it doesn't already exist.
    /// For MongoDB: Creates Atlas Vector Search index
    /// For Qdrant: Creates collection with proper vector configuration
    /// </summary>
    /// <param name="collectionName">The name of the collection to create an index for</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the async operation</returns>
    Task CreateIndexAsync(string collectionName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a search index exists for the specified collection.
    /// For MongoDB: Checks if Atlas Vector Search index exists
    /// For Qdrant: Checks if collection exists
    /// </summary>
    /// <param name="collectionName">The name of the collection to check</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if the index/collection exists, false otherwise</returns>
    Task<bool> IndexExistsAsync(string collectionName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes the search index for the specified collection.
    /// For MongoDB: Deletes Atlas Vector Search index
    /// For Qdrant: Deletes collection
    /// </summary>
    /// <param name="collectionName">The name of the collection to delete the index for</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the async operation</returns>
    Task DeleteIndexAsync(string collectionName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the unique identifier for the index/collection.
    /// For MongoDB: Returns Atlas index ID
    /// For Qdrant: Returns collection name (collections are identified by name)
    /// </summary>
    /// <param name="collectionName">The name of the collection</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The index/collection identifier, or null if not found</returns>
    Task<string?> GetIndexIdAsync(string collectionName, CancellationToken cancellationToken = default);
}