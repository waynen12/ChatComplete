using Knowledge.Contracts; // contains KnowledgeSummaryDto

namespace KnowledgeEngine.Persistence;

/// <summary>
/// Read-only access to stored knowledge collections (vector stores).
/// </summary>
public interface IKnowledgeRepository
{
    /// <summary>
    /// Returns a summary entry for every knowledge collection
    /// currently stored in MongoDB / the vector store.
    /// </summary>
    Task<IEnumerable<KnowledgeSummaryDto>> GetAllAsync(
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Checks whether a collection with the given ID exists.
    /// </summary>
    Task<bool> ExistsAsync(string collectionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a knowledge collection from MongoDB / the vector store.
    /// </summary>
    Task DeleteAsync(string collectionId, CancellationToken ct = default);

    /// <summary>
    /// Creates or updates a knowledge collection record with metadata.
    /// Called when documents are uploaded to track collection information.
    /// </summary>
    Task<string> CreateOrUpdateCollectionAsync(
        string collectionId, 
        string name, 
        string? description = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Increments document and chunk counts for a collection.
    /// Called after successful document processing to track statistics.
    /// </summary>
    /// <param name="collectionId">The collection identifier</param>
    /// <param name="documentCount">Number of documents to add to the count</param>
    /// <param name="chunkCount">Number of chunks to add to the count</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task UpdateCollectionStatsAsync(
        string collectionId,
        int documentCount,
        int chunkCount,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all documents within a specific knowledge collection.
    /// Returns metadata only (no chunk content).
    /// </summary>
    /// <param name="collectionId">The collection identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of document metadata entries</returns>
    Task<IEnumerable<DocumentMetadataDto>> GetDocumentsByCollectionAsync(
        string collectionId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all chunks for a specific document, ordered by chunk index.
    /// Used to reconstruct the full document text.
    /// </summary>
    /// <param name="collectionId">The collection identifier</param>
    /// <param name="documentId">The document identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Ordered list of document chunks</returns>
    Task<IEnumerable<DocumentChunkDto>> GetDocumentChunksAsync(
        string collectionId,
        string documentId,
        CancellationToken cancellationToken = default);
}
