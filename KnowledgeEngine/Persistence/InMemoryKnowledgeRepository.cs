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
    /// Adds or updates a knowledge collection summary in the store
    /// This method is called internally when knowledge is uploaded
    /// </summary>
    public void AddOrUpdateCollection(string collectionId, KnowledgeSummaryDto summary)
    {
        _knowledgeStore[collectionId] = summary;
    }
}