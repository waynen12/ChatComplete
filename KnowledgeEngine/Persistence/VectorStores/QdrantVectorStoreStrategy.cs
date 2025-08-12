using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ChatCompletion.Config;
using KnowledgeEngine.Logging;
using KnowledgeEngine.Models;
using KnowledgeEngine.Persistence.IndexManagers;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel.Connectors.Qdrant;

namespace KnowledgeEngine.Persistence.VectorStores;

/// <summary>
/// Qdrant implementation of vector store strategy using Semantic Kernel.
/// Provides vector operations equivalent to MongoDB Atlas implementation.
/// </summary>
public class QdrantVectorStoreStrategy : IVectorStoreStrategy
{
    private readonly QdrantVectorStore _vectorStore;
    private readonly QdrantSettings _settings;
    private readonly IIndexManager _indexManager;

    public QdrantVectorStoreStrategy(
        QdrantVectorStore vectorStore,
        QdrantSettings settings,
        IIndexManager indexManager
    )
    {
        _vectorStore = vectorStore ?? throw new ArgumentNullException(nameof(vectorStore));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _indexManager = indexManager ?? throw new ArgumentNullException(nameof(indexManager));
    }

    /// <summary>
    /// Upserts a chunk into Qdrant using Semantic Kernel
    /// </summary>
    public async Task UpsertAsync(
        string collectionName,
        string key,
        string text,
        Embedding<float> embedding,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            // Ensure collection exists before upserting
            if (!await _indexManager.IndexExistsAsync(collectionName, cancellationToken))
            {
                LoggerProvider.Logger.Information(
                    "Creating Qdrant collection {Collection} as it does not exist",
                    collectionName
                );
                await _indexManager.CreateIndexAsync(collectionName, cancellationToken);
            }

            // Parse chunk order from key (format: "fileId-p0001")
            var chunkOrder = 0;
            var source = key;
            if (key.Contains("-p"))
            {
                var parts = key.Split("-p");
                source = parts[0];
                if (parts.Length > 1 && int.TryParse(parts[1], out var order))
                {
                    chunkOrder = order;
                }
            }

            // Get collection reference from vector store (using Guid keys)
            var collection = _vectorStore.GetCollection<Guid, QdrantRecord>(collectionName);

            // Generate a deterministic GUID from the string key for consistent mapping
            var guidId = CreateDeterministicGuid(key);

            // Create record matching MongoDB document structure
            var record = new QdrantRecord
            {
                Id = guidId,
                DocumentKey = key, // Store original string key for lookup
                Text = text,
                Vector = embedding.Vector.ToArray(),
                Source = source,
                ChunkOrder = chunkOrder,
                Tags = string.Empty,
            };

            // Upsert the record (Semantic Kernel handles the REST API calls)
            await collection.UpsertAsync(record, cancellationToken: cancellationToken);

            LoggerProvider.Logger.Information(
                "Successfully upserted chunk {Key} to Qdrant collection {Collection}",
                key,
                collectionName
            );
        }
        catch (Exception ex)
        {
            LoggerProvider.Logger.Error(
                ex,
                "Failed to upsert chunk {Key} to Qdrant collection {Collection}",
                key,
                collectionName
            );
            throw;
        }
    }

    /// <summary>
    /// Performs semantic search in Qdrant using Semantic Kernel's SearchAsync API
    /// </summary>
    public async Task<List<KnowledgeSearchResult>> SearchAsync(
        string collectionName,
        string query,
        Embedding<float> embedding,
        int limit = 10,
        double minRelevanceScore = 0.6,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            // Check if collection exists before searching
            if (!await _indexManager.IndexExistsAsync(collectionName, cancellationToken))
            {
                LoggerProvider.Logger.Warning(
                    "Cannot search in Qdrant collection {Collection} - collection does not exist",
                    collectionName
                );
                return new List<KnowledgeSearchResult>();
            }

            // Get collection reference from vector store (using Guid keys)
            var collection = _vectorStore.GetCollection<Guid, QdrantRecord>(collectionName);

            // Configure search options (without Top property)
            var searchOptions = new VectorSearchOptions<QdrantRecord>()
            {
                IncludeVectors = false, // For performance, we don't need vectors in results
            };

            // Perform vector search using embedding with correct signature
            var searchResults = collection.SearchAsync(
                embedding.Vector, // The search vector
                top: limit, // Number of results to return
                searchOptions, // Search options
                cancellationToken
            ); // Cancellation token

            var results = new List<KnowledgeSearchResult>();

            // Convert search results and apply minimum relevance score filter
            await foreach (var result in searchResults.WithCancellation(cancellationToken))
            {
                if ((result.Score ?? 0.0) >= minRelevanceScore && result.Record != null)
                {
                    results.Add(
                        new KnowledgeSearchResult
                        {
                            Text = result.Record.Text,
                            Source = result.Record.Source,
                            ChunkOrder = result.Record.ChunkOrder,
                            Tags = result.Record.Tags,
                            Score = result.Score ?? 0.0,
                        }
                    );
                }
            }

            // Sort by score descending (highest relevance first)
            var sortedResults = results.OrderByDescending(r => r.Score).ToList();

            LoggerProvider.Logger.Information(
                "Qdrant vector search for query '{Query}' returned {Count} results above score {MinScore}",
                query,
                sortedResults.Count,
                minRelevanceScore
            );

            return sortedResults;
        }
        catch (Exception ex)
        {
            LoggerProvider.Logger.Error(
                ex,
                "Failed to perform Qdrant vector search for query: {Query}",
                query
            );
            return new List<KnowledgeSearchResult>();
        }
    }

    /// <summary>
    /// Lists all available collections in Qdrant
    /// </summary>
    public async Task<List<string>> ListCollectionsAsync(
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            // Use vector store to list collections
            var collectionNames = new List<string>();
            await foreach (var name in _vectorStore.ListCollectionNamesAsync(cancellationToken))
            {
                collectionNames.Add(name);
            }
            return collectionNames;
        }
        catch (Exception ex)
        {
            LoggerProvider.Logger.Error(ex, "Failed to list Qdrant collections");
            return new List<string>();
        }
    }

    /// <summary>
    /// Deletes a collection and all its data from Qdrant
    /// </summary>
    public async Task DeleteCollectionAsync(string collectionName, CancellationToken cancellationToken = default)
    {
        try
        {
            LoggerProvider.Logger.Information("Deleting Qdrant collection: {CollectionName}", collectionName);
            
            // Check if collection exists first
            var collections = await ListCollectionsAsync(cancellationToken);
            if (!collections.Contains(collectionName))
            {
                LoggerProvider.Logger.Information("Collection {CollectionName} does not exist in Qdrant, skipping deletion", collectionName);
                return;
            }

            // Delete the collection using the vector store collection interface
            var collection = _vectorStore.GetCollection<Guid, QdrantRecord>(collectionName);
            await collection.EnsureCollectionDeletedAsync(cancellationToken);
            
            LoggerProvider.Logger.Information("Successfully deleted Qdrant collection: {CollectionName}", collectionName);
        }
        catch (Exception ex)
        {
            LoggerProvider.Logger.Error(ex, "Failed to delete Qdrant collection: {CollectionName}", collectionName);
            throw;
        }
    }

    /// <summary>
    /// Creates a deterministic GUID from a string key using MD5 hash (matches Python implementation)
    /// </summary>
    private static Guid CreateDeterministicGuid(string input)
    {
        using var md5 = MD5.Create();
        var hash = md5.ComputeHash(Encoding.UTF8.GetBytes(input));

        // Convert to string and back to match Python uuid.UUID(bytes=...) behavior
        // Python's UUID constructor interprets bytes differently than .NET's Guid constructor
        var uuidString =
            $"{hash[0]:x2}{hash[1]:x2}{hash[2]:x2}{hash[3]:x2}-"
            + $"{hash[4]:x2}{hash[5]:x2}-"
            + $"{hash[6]:x2}{hash[7]:x2}-"
            + $"{hash[8]:x2}{hash[9]:x2}-"
            + $"{hash[10]:x2}{hash[11]:x2}{hash[12]:x2}{hash[13]:x2}{hash[14]:x2}{hash[15]:x2}";

        return Guid.Parse(uuidString);
    }
}
