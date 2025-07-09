using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
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
}
