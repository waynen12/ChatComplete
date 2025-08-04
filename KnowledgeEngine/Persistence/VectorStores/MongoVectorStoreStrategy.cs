using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ChatCompletion.Config;
using KnowledgeEngine.Logging;
using KnowledgeEngine.Models;
using Microsoft.Extensions.AI;
using MongoDB.Bson;
using MongoDB.Driver;

namespace KnowledgeEngine.Persistence.VectorStores;

/// <summary>
/// MongoDB Atlas implementation of vector store strategy.
/// Extracted from existing KnowledgeManager MongoDB operations.
/// </summary>
public class MongoVectorStoreStrategy : IVectorStoreStrategy
{
    private readonly IMongoDatabase _mongoDatabase;
    private readonly MongoAtlasSettings _atlasSettings;

    public MongoVectorStoreStrategy(IMongoDatabase mongoDatabase, MongoAtlasSettings atlasSettings)
    {
        _mongoDatabase = mongoDatabase ?? throw new ArgumentNullException(nameof(mongoDatabase));
        _atlasSettings = atlasSettings ?? throw new ArgumentNullException(nameof(atlasSettings));
    }

    /// <summary>
    /// Upserts a chunk into MongoDB Atlas using direct MongoDB driver.
    /// Extracted from KnowledgeManager.UpsertAsync()
    /// </summary>
    public async Task UpsertAsync(
        string collectionName,
        string key,
        string text,
        Embedding<float> embedding,
        CancellationToken cancellationToken = default)
    {
        try
        {
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
            
            // Create MongoDB document matching existing structure
            var document = new BsonDocument
            {
                ["_id"] = key,
                ["text"] = text,
                ["vector"] = new BsonArray(embedding.Vector.ToArray()),
                ["source"] = source,
                ["chunkOrder"] = chunkOrder,
                ["tags"] = ""
            };

            // Get MongoDB collection directly
            var mongoCollection = _mongoDatabase.GetCollection<BsonDocument>(collectionName);
            
            // Use ReplaceOne with upsert option
            var filter = Builders<BsonDocument>.Filter.Eq("_id", key);
            var options = new ReplaceOptions { IsUpsert = true };
            
            await mongoCollection.ReplaceOneAsync(filter, document, options, cancellationToken);
            
            LoggerProvider.Logger.Information(
                "Successfully upserted chunk {Key} to MongoDB collection {Collection}",
                key, collectionName);
        }
        catch (Exception ex)
        {
            LoggerProvider.Logger.Error(ex,
                "Failed to upsert chunk {Key} to MongoDB collection {Collection}",
                key, collectionName);
            throw;
        }
    }

    /// <summary>
    /// Searches MongoDB Atlas using vector search aggregation pipeline.
    /// Extracted from KnowledgeManager.SearchAsync()
    /// </summary>
    public async Task<List<KnowledgeSearchResult>> SearchAsync(
        string collectionName,
        string query,
        Embedding<float> embedding,
        int limit = 10,
        double minRelevanceScore = 0.6,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Get MongoDB collection directly for vector search
            var mongoCollection = _mongoDatabase.GetCollection<BsonDocument>(collectionName);
            
            // Build MongoDB Atlas Vector Search aggregation pipeline
            var vectorSearchStage = new BsonDocument("$vectorSearch", new BsonDocument
            {
                ["index"] = _atlasSettings.SearchIndexName,
                ["path"] = "vector",
                ["queryVector"] = new BsonArray(embedding.Vector.ToArray()),
                ["numCandidates"] = limit * 10, // Search more candidates for better results
                ["limit"] = limit
            });

            var projectStage = new BsonDocument("$project", new BsonDocument
            {
                ["_id"] = 1,
                ["text"] = 1,
                ["source"] = 1,
                ["chunkOrder"] = 1,
                ["tags"] = 1,
                ["score"] = new BsonDocument("$meta", "vectorSearchScore")
            });

            var pipeline = new[] { vectorSearchStage, projectStage };
            
            var searchResults = new List<KnowledgeSearchResult>();
            
            using var cursor = await mongoCollection.AggregateAsync<BsonDocument>(
                pipeline, cancellationToken: cancellationToken);
            
            while (await cursor.MoveNextAsync(cancellationToken))
            {
                foreach (var doc in cursor.Current)
                {
                    var score = doc.Contains("score") ? doc["score"].AsDouble : 0.0;
                    
                    // Apply minimum relevance score filter
                    if (score >= minRelevanceScore)
                    {
                        searchResults.Add(new KnowledgeSearchResult
                        {
                            Text = doc.GetValue("text", "").AsString,
                            Source = doc.GetValue("source", "").AsString,
                            ChunkOrder = doc.GetValue("chunkOrder", 0).AsInt32,
                            Tags = doc.GetValue("tags", "").AsString,
                            Score = score
                        });
                    }
                }
            }

            LoggerProvider.Logger.Information(
                "MongoDB vector search for query '{Query}' returned {Count} results above score {MinScore}",
                query, searchResults.Count, minRelevanceScore);

            return searchResults.OrderByDescending(r => r.Score).ToList();
        }
        catch (Exception ex)
        {
            LoggerProvider.Logger.Error(ex,
                "Failed to perform MongoDB vector search for query: {Query}",
                query);
            return new List<KnowledgeSearchResult>();
        }
    }

    /// <summary>
    /// Lists all available collections in MongoDB
    /// </summary>
    public async Task<List<string>> ListCollectionsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var cursor = await _mongoDatabase.ListCollectionNamesAsync(cancellationToken: cancellationToken);
            var collectionNames = await cursor.ToListAsync(cancellationToken);
            
            // Filter out system collections and conversations
            return collectionNames
                .Where(n => !n.StartsWith("system.", StringComparison.OrdinalIgnoreCase) 
                           && !n.Equals("conversations", StringComparison.OrdinalIgnoreCase))
                .ToList();
        }
        catch (Exception ex)
        {
            LoggerProvider.Logger.Error(ex, "Failed to list MongoDB collections");
            return new List<string>();
        }
    }
}