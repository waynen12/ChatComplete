using Knowledge.Contracts;
using KnowledgeEngine.Persistence.VectorStores;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;

namespace KnowledgeEngine.Persistence.Sqlite.Repositories;

/// <summary>
/// SQLite implementation of IKnowledgeRepository for persistent metadata storage
/// Replaces the in-memory repository with full database persistence
/// </summary>
public class SqliteKnowledgeRepository : IKnowledgeRepository
{
    private readonly SqliteDbContext _dbContext;
    private readonly IVectorStoreStrategy _vectorStore;
    private readonly ILogger<SqliteKnowledgeRepository> _logger;

    public SqliteKnowledgeRepository(
        SqliteDbContext dbContext, 
        IVectorStoreStrategy vectorStore,
        ILogger<SqliteKnowledgeRepository> logger)
    {
        _dbContext = dbContext;
        _vectorStore = vectorStore;
        _logger = logger;
    }

    public async Task<IEnumerable<KnowledgeSummaryDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT CollectionId, Name, DocumentCount
            FROM KnowledgeCollections 
            WHERE Status = 'Active'
            ORDER BY UpdatedAt DESC
            """;

        var results = new List<KnowledgeSummaryDto>();
        var connection = await _dbContext.GetConnectionAsync();

        using var command = new SqliteCommand(sql, connection);
        using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(new KnowledgeSummaryDto
            {
                Id = reader.GetString(0),
                Name = reader.GetString(1),
                DocumentCount = reader.GetInt32(2)
            });
        }

        return results;
    }

    public async Task<bool> ExistsAsync(string collectionId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT COUNT(1) 
            FROM KnowledgeCollections 
            WHERE CollectionId = @collectionId AND Status = 'Active'
            """;

        var connection = await _dbContext.GetConnectionAsync();
        using var command = new SqliteCommand(sql, connection);
        command.Parameters.AddWithValue("@collectionId", collectionId);

        var count = Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));
        return count > 0;
    }

    public async Task DeleteAsync(string collectionId, CancellationToken cancellationToken = default)
    {
        try
        {
            // 1. Delete the actual vector store collection (Qdrant collection)
            _logger.LogInformation("Deleting vector store collection: {CollectionId}", collectionId);
            await _vectorStore.DeleteCollectionAsync(collectionId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to delete vector store collection {CollectionId}, continuing with metadata cleanup", collectionId);
        }

        // 2. Mark the collection as deleted in SQLite metadata
        const string sql = """
            UPDATE KnowledgeCollections 
            SET Status = 'Deleted', UpdatedAt = CURRENT_TIMESTAMP 
            WHERE CollectionId = @collectionId
            """;

        var connection = await _dbContext.GetConnectionAsync();
        using var command = new SqliteCommand(sql, connection);
        command.Parameters.AddWithValue("@collectionId", collectionId);

        await command.ExecuteNonQueryAsync(cancellationToken);
        
        _logger.LogInformation("Successfully deleted collection metadata for: {CollectionId}", collectionId);
    }

    /// <summary>
    /// Creates or updates a knowledge collection record
    /// Called when documents are uploaded to track metadata
    /// </summary>
    public async Task<string> CreateOrUpdateCollectionAsync(
        string collectionId, 
        string name, 
        string? description = null,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO KnowledgeCollections (CollectionId, Name, Description, Status, CreatedAt, UpdatedAt)
            VALUES (@collectionId, @name, @description, 'Active', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP)
            ON CONFLICT(CollectionId) DO UPDATE SET
                Name = @name,
                Description = COALESCE(@description, Description),
                Status = 'Active',
                UpdatedAt = CURRENT_TIMESTAMP
            """;

        var connection = await _dbContext.GetConnectionAsync();
        using var command = new SqliteCommand(sql, connection);
        command.Parameters.AddWithValue("@collectionId", collectionId);
        command.Parameters.AddWithValue("@name", name);
        command.Parameters.AddWithValue("@description", description ?? (object)DBNull.Value);

        await command.ExecuteNonQueryAsync(cancellationToken);
        return collectionId;
    }

    /// <summary>
    /// Updates document and chunk counts for a collection
    /// Called after successful document processing
    /// </summary>
    public async Task UpdateCollectionStatsAsync(
        string collectionId, 
        int documentCount, 
        int chunkCount,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE KnowledgeCollections 
            SET DocumentCount = DocumentCount + @documentCount,
                ChunkCount = ChunkCount + @chunkCount,
                UpdatedAt = CURRENT_TIMESTAMP
            WHERE CollectionId = @collectionId
            """;

        var connection = await _dbContext.GetConnectionAsync();
        using var command = new SqliteCommand(sql, connection);
        command.Parameters.AddWithValue("@collectionId", collectionId);
        command.Parameters.AddWithValue("@documentCount", documentCount);
        command.Parameters.AddWithValue("@chunkCount", chunkCount);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    /// <summary>
    /// Adds a document record to track uploaded files
    /// </summary>
    public async Task<string> AddDocumentAsync(
        string collectionId,
        string documentId,
        string fileName,
        long fileSize,
        string fileType,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO KnowledgeDocuments
            (CollectionId, DocumentId, OriginalFileName, FileSize, FileType, UploadedAt)
            VALUES (@collectionId, @documentId, @fileName, @fileSize, @fileType, CURRENT_TIMESTAMP)
            """;

        var connection = await _dbContext.GetConnectionAsync();
        using var command = new SqliteCommand(sql, connection);
        command.Parameters.AddWithValue("@collectionId", collectionId);
        command.Parameters.AddWithValue("@documentId", documentId);
        command.Parameters.AddWithValue("@fileName", fileName);
        command.Parameters.AddWithValue("@fileSize", fileSize);
        command.Parameters.AddWithValue("@fileType", fileType);

        await command.ExecuteNonQueryAsync(cancellationToken);
        return documentId;
    }

    /// <summary>
    /// Gets all documents within a specific knowledge collection.
    /// Returns metadata only (no chunk content).
    /// </summary>
    public async Task<IEnumerable<DocumentMetadataDto>> GetDocumentsByCollectionAsync(
        string collectionId,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT DocumentId, OriginalFileName, ChunkCount, FileSize, FileType, UploadedAt
            FROM KnowledgeDocuments
            WHERE CollectionId = @collectionId AND ProcessingStatus = 'Completed'
            ORDER BY UploadedAt DESC
            """;

        var results = new List<DocumentMetadataDto>();
        var connection = await _dbContext.GetConnectionAsync();

        using var command = new SqliteCommand(sql, connection);
        command.Parameters.AddWithValue("@collectionId", collectionId);

        using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(new DocumentMetadataDto
            {
                DocumentId = reader.GetString(0),
                OriginalFileName = reader.GetString(1),
                ChunkCount = reader.GetInt32(2),
                FileSize = reader.GetInt64(3),
                FileType = reader.GetString(4),
                UploadedAt = reader.GetDateTime(5)
            });
        }

        _logger.LogDebug("Found {Count} documents in collection {CollectionId}", results.Count, collectionId);
        return results;
    }

    /// <summary>
    /// Gets all chunks for a specific document, ordered by chunk index.
    /// Used to reconstruct the full document text.
    /// </summary>
    public async Task<IEnumerable<DocumentChunkDto>> GetDocumentChunksAsync(
        string collectionId,
        string documentId,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT ChunkId, ChunkText, ChunkOrder, TokenCount, CharacterCount
            FROM KnowledgeChunks
            WHERE CollectionId = @collectionId AND DocumentId = @documentId
            ORDER BY ChunkOrder ASC
            """;

        var results = new List<DocumentChunkDto>();
        var connection = await _dbContext.GetConnectionAsync();

        using var command = new SqliteCommand(sql, connection);
        command.Parameters.AddWithValue("@collectionId", collectionId);
        command.Parameters.AddWithValue("@documentId", documentId);

        using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(new DocumentChunkDto
            {
                ChunkId = reader.GetString(0),
                ChunkText = reader.GetString(1),
                ChunkOrder = reader.GetInt32(2),
                TokenCount = reader.GetInt32(3),
                CharacterCount = reader.GetInt32(4)
            });
        }

        _logger.LogDebug("Retrieved {Count} chunks for document {DocumentId} in collection {CollectionId}",
            results.Count, documentId, collectionId);
        return results;
    }
}