using System.Collections.Concurrent;
using Knowledge.Contracts;

namespace KnowledgeEngine.Persistence;

/// <summary>
/// In-memory implementation of IKnowledgeRepository for Qdrant deployments
/// This provides basic operations without requiring MongoDB
/// Note: Data will be lost when container restarts - for production consider persistent storage
/// </summary>
public class InMemoryKnowledgeRepository : IKnowledgeRepository
{
    private readonly ConcurrentDictionary<string, KnowledgeSummaryDto> _knowledgeStore = new();

    public Task<IEnumerable<KnowledgeSummaryDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var summaries = _knowledgeStore.Values.ToList();
        return Task.FromResult<IEnumerable<KnowledgeSummaryDto>>(summaries);
    }

    public Task<bool> ExistsAsync(string collectionId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_knowledgeStore.ContainsKey(collectionId));
    }

    public Task DeleteAsync(string collectionId, CancellationToken cancellationToken = default)
    {
        _knowledgeStore.TryRemove(collectionId, out _);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Creates or updates a knowledge collection record with metadata
    /// </summary>
    public Task<string> CreateOrUpdateCollectionAsync(
        string collectionId, 
        string name, 
        string? description = null,
        CancellationToken cancellationToken = default)
    {
        var summary = new KnowledgeSummaryDto
        {
            Id = collectionId,
            Name = name,
            DocumentCount = _knowledgeStore.ContainsKey(collectionId) ? _knowledgeStore[collectionId].DocumentCount : 0
        };
        _knowledgeStore[collectionId] = summary;
        return Task.FromResult(collectionId);
    }

    /// <summary>
    /// Updates document and chunk counts for a collection
    /// </summary>
    public Task UpdateCollectionStatsAsync(
        string collectionId, 
        int documentCount, 
        int chunkCount,
        CancellationToken cancellationToken = default)
    {
        if (_knowledgeStore.TryGetValue(collectionId, out var existing))
        {
            // Increment the existing counts
            _knowledgeStore[collectionId] = new KnowledgeSummaryDto
            {
                Id = existing.Id,
                Name = existing.Name,
                DocumentCount = existing.DocumentCount + documentCount
            };
        }
        return Task.CompletedTask;
    }

    /// <summary>
    /// Adds or updates a knowledge collection summary in the store
    /// This method is called internally when knowledge is uploaded
    /// </summary>
    public void AddOrUpdateCollection(string collectionId, KnowledgeSummaryDto summary)
    {
        _knowledgeStore[collectionId] = summary;
    }

    /// <summary>
    /// Gets all documents within a specific knowledge collection.
    /// NOT SUPPORTED in InMemoryKnowledgeRepository - use SqliteKnowledgeRepository for document tracking.
    /// </summary>
    public Task<IEnumerable<DocumentMetadataDto>> GetDocumentsByCollectionAsync(
        string collectionId,
        CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException(
            "Document tracking is not supported in InMemoryKnowledgeRepository. " +
            "Use SqliteKnowledgeRepository for full document and chunk management.");
    }

    /// <summary>
    /// Gets all chunks for a specific document.
    /// NOT SUPPORTED in InMemoryKnowledgeRepository - use SqliteKnowledgeRepository for chunk retrieval.
    /// </summary>
    public Task<IEnumerable<DocumentChunkDto>> GetDocumentChunksAsync(
        string collectionId,
        string documentId,
        CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException(
            "Chunk retrieval is not supported in InMemoryKnowledgeRepository. " +
            "Use SqliteKnowledgeRepository for full document and chunk management.");
    }
}