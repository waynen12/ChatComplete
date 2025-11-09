using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Knowledge.Contracts;
using KnowledgeEngine.Persistence.IndexManagers;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using Serilog;

namespace KnowledgeEngine.Persistence;

public sealed partial class MongoKnowledgeRepository : IKnowledgeRepository
{
    private readonly IMongoDatabase _database;
    private readonly ILogger<MongoKnowledgeRepository> _log;
    private AtlasIndexManager _indexManager;

    public MongoKnowledgeRepository(IMongoDatabase database, ILogger<MongoKnowledgeRepository> log, AtlasIndexManager indexManager)
    {
        _database = database;
        _log = log;
        _indexManager = indexManager;
        
    }

    /// <summary>Returns one summary entry per vector-store collection.</summary>
    public async Task<IEnumerable<KnowledgeSummaryDto>> GetAllAsync(
        CancellationToken cancellationToken = default
    )
    {
        // 1 - list every collection name in this database
        using var cursor = await _database.ListCollectionNamesAsync(
            cancellationToken: cancellationToken
        );

        var collectionNames = await cursor.ToListAsync(cancellationToken);

        collectionNames = collectionNames.Where(n => !n.StartsWith("system.", StringComparison.OrdinalIgnoreCase) && !n.Equals("conversations", StringComparison.OrdinalIgnoreCase)).ToList();

        // 2 - project each collection into a KnowledgeSummaryDto
        var summaries = new List<KnowledgeSummaryDto>();

        foreach (var name in collectionNames)
        {
            var collection = _database.GetCollection<BsonDocument>(name);

            // quick metadata query — no filter
            var docCount = (int)
                await collection.CountDocumentsAsync(
                    FilterDefinition<BsonDocument>.Empty,
                    cancellationToken: cancellationToken
                );

            summaries.Add(
                new KnowledgeSummaryDto
                {
                    Id = name, // use collection name as Id
                    Name = name, // human-friendly alias could be added later
                    DocumentCount = docCount,
                }
            );
        }
        _log.LogInformation("Listing names…");
        _log.LogInformation("Found collections: {Names}", string.Join(",", collectionNames));
        return summaries.OrderBy(s => s.Name); // optional sort
    }

    /// <summary>Checks if a particular collection exists.</summary>
    public async Task<bool> ExistsAsync(
        string collectionId,
        CancellationToken cancellationToken = default
    )
    {
        var filter = new BsonDocument("name", collectionId);

        using var cursor = await _database.ListCollectionsAsync(
            cancellationToken: cancellationToken
        );

        return await cursor.AnyAsync(cancellationToken);
    }

    /// <summary>
    /// Deletes a knowledge collection from the database.
    /// </summary>
    /// <param name="collectionId"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    public async Task DeleteAsync(string collectionId, CancellationToken ct = default)
    {
        // drop the vector-store collection itself
        await _database.DropCollectionAsync(collectionId, ct);

        // drop associated search index (ignore if not present)
        try
        {
            await _indexManager.DeleteIndexAsync(collectionId, ct);
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "Index drop failed for {Collection}", collectionId);
        }
    }

    /// <summary>
    /// For MongoDB, collections are created automatically when documents are inserted.
    /// This method is a no-op but maintains interface compatibility.
    /// </summary>
    public async Task<string> CreateOrUpdateCollectionAsync(
        string collectionId, 
        string name, 
        string? description = null,
        CancellationToken cancellationToken = default)
    {
        // MongoDB collections are created automatically when documents are inserted
        // No explicit creation needed, just return the collection ID
        _log.LogDebug("MongoDB collection {CollectionId} will be created automatically on first document insert", collectionId);
        return await Task.FromResult(collectionId);
    }

    /// <summary>
    /// For MongoDB, document counts are calculated dynamically from actual collection data.
    /// This method is a no-op but maintains interface compatibility.
    /// </summary>
    public async Task UpdateCollectionStatsAsync(
        string collectionId,
        int documentCount,
        int chunkCount,
        CancellationToken cancellationToken = default)
    {
        // MongoDB calculates document counts dynamically in GetAllAsync
        // No need to store separate stats, just log the incremental operation
        _log.LogDebug("MongoDB collection {CollectionId} incremented by: {DocumentCount} documents, {ChunkCount} chunks",
            collectionId, documentCount, chunkCount);
        await Task.CompletedTask;
    }

    /// <summary>
    /// Gets all documents within a specific knowledge collection.
    /// NOT SUPPORTED in MongoKnowledgeRepository - use SqliteKnowledgeRepository for document tracking.
    /// </summary>
    public Task<IEnumerable<DocumentMetadataDto>> GetDocumentsByCollectionAsync(
        string collectionId,
        CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException(
            "Document tracking is not supported in MongoKnowledgeRepository. " +
            "Use SqliteKnowledgeRepository for full document and chunk management.");
    }

    /// <summary>
    /// Gets all chunks for a specific document.
    /// NOT SUPPORTED in MongoKnowledgeRepository - use SqliteKnowledgeRepository for chunk retrieval.
    /// </summary>
    public Task<IEnumerable<DocumentChunkDto>> GetDocumentChunksAsync(
        string collectionId,
        string documentId,
        CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException(
            "Chunk retrieval is not supported in MongoKnowledgeRepository. " +
            "Use SqliteKnowledgeRepository for full document and chunk management.");
    }

    /// <summary>
    /// Adds a document record to track uploaded files.
    /// NOT SUPPORTED in MongoKnowledgeRepository - use SqliteKnowledgeRepository for document tracking.
    /// </summary>
    public Task<string> AddDocumentAsync(
        string collectionId,
        string documentId,
        string fileName,
        long fileSize,
        string fileType,
        CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException(
            "Document tracking is not supported in MongoKnowledgeRepository. " +
            "Use SqliteKnowledgeRepository for full document and chunk management.");
    }
}
