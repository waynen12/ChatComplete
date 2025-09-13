using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ChatCompletion.Config;
using KnowledgeEngine.Logging;
using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel.Connectors.Qdrant;
using Microsoft.SemanticKernel.Data;

namespace KnowledgeEngine.Persistence.IndexManagers;

/// <summary>
/// Index manager for Qdrant vector store using Semantic Kernel abstractions.
/// In Qdrant, "indexes" are collections with vector configuration.
/// </summary>
public class QdrantIndexManager : IIndexManager
{
    private readonly QdrantVectorStore _vectorStore;
    private readonly QdrantSettings _settings;
    private readonly HttpClient _httpClient;

    public QdrantIndexManager(QdrantVectorStore vectorStore, QdrantSettings settings)
    {
        _vectorStore = vectorStore ?? throw new ArgumentNullException(nameof(vectorStore));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _httpClient = new HttpClient();
    }

    /// <summary>
    /// Creates a Qdrant collection with correct 768 dimensions using direct REST API
    /// </summary>
    public async Task CreateIndexAsync(string collectionName, CancellationToken cancellationToken = default)
    {
        try
        {
            // Check if collection already exists
            if (await IndexExistsAsync(collectionName, cancellationToken))
            {
                LoggerProvider.Logger.Information(
                    "Qdrant collection '{CollectionName}' already exists",
                    collectionName);
                return;
            }

            // Create collection manually via REST API with correct 768 dimensions
            // Note: REST API uses port 6333, while gRPC (for data operations) uses port 6334
            var restApiUrl = $"http://{_settings.Host}:6333/collections/{collectionName}";
            
            var collectionConfig = new
            {
                vectors = new
                {
                    size = _settings.VectorSize, // This should be 768 from config
                    distance = _settings.DistanceMetric // This should be "Cosine"
                }
            };

            var json = JsonSerializer.Serialize(collectionConfig);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PutAsync(restApiUrl, content, cancellationToken);
            
            if (response.IsSuccessStatusCode)
            {
                LoggerProvider.Logger.Information(
                    "Successfully created Qdrant collection '{CollectionName}' with {VectorSize} dimensions via REST API",
                    collectionName, _settings.VectorSize);
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                throw new InvalidOperationException(
                    $"Failed to create Qdrant collection via REST API. Status: {response.StatusCode}, Error: {errorContent}");
            }
        }
        catch (Exception ex)
        {
            LoggerProvider.Logger.Error(ex,
                "Failed to create Qdrant collection '{CollectionName}'",
                collectionName);
            throw;
        }
    }

    /// <summary>
    /// Checks if a Qdrant collection exists using Semantic Kernel
    /// </summary>
    public async Task<bool> IndexExistsAsync(string collectionName, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _vectorStore.CollectionExistsAsync(collectionName, cancellationToken);
        }
        catch (Exception ex)
        {
            LoggerProvider.Logger.Error(ex,
                "Exception occurred while checking if Qdrant collection '{CollectionName}' exists",
                collectionName);
            return false;
        }
    }

    /// <summary>
    /// Deletes a Qdrant collection using Semantic Kernel
    /// </summary>
    public async Task DeleteIndexAsync(string collectionName, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!await IndexExistsAsync(collectionName, cancellationToken))
            {
                LoggerProvider.Logger.Information(
                    "Qdrant collection '{CollectionName}' does not exist. Cannot delete.",
                    collectionName);
                return;
            }

            var collection = _vectorStore.GetCollection<Guid, QdrantRecord>(collectionName);
            await collection.EnsureCollectionDeletedAsync(cancellationToken);

            LoggerProvider.Logger.Information(
                "Successfully deleted Qdrant collection '{CollectionName}'",
                collectionName);
        }
        catch (Exception ex)
        {
            LoggerProvider.Logger.Error(ex,
                "Failed to delete Qdrant collection '{CollectionName}'",
                collectionName);
            throw;
        }
    }

    /// <summary>
    /// Gets the collection name (Qdrant collections are identified by name)
    /// </summary>
    public async Task<string?> GetIndexIdAsync(string collectionName, CancellationToken cancellationToken = default)
    {
        var exists = await IndexExistsAsync(collectionName, cancellationToken);
        return exists ? collectionName : null;
    }
}

/// <summary>
/// Record type for Qdrant collections to match MongoDB document structure
/// </summary>
public record QdrantRecord
{
    [VectorStoreKey]
    public Guid Id { get; init; } = Guid.Empty;
    
    public string DocumentKey { get; init; } = string.Empty;  // Store original string key for lookup
    
    [VectorStoreData]
    public string Text { get; init; } = string.Empty;
    
    [VectorStoreVector(768)]  // 768-dimensional vectors from OpenAI text-embedding-ada-002
    public ReadOnlyMemory<float> Vector { get; init; }
    
    [VectorStoreData]
    public string Source { get; init; } = string.Empty;
    
    [VectorStoreData]
    public int ChunkOrder { get; init; }
    
    [VectorStoreData]
    public string Tags { get; init; } = string.Empty;
}