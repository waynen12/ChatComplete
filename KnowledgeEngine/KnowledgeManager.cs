using System.Diagnostics;
using ChatCompletion.Config;
using KnowledgeEngine.Logging;
using KnowledgeEngine.Models;
using Microsoft.Extensions.AI;
using Microsoft.SemanticKernel.Connectors.MongoDB;
using Microsoft.SemanticKernel.Text;
using MongoDB.Driver;

#pragma warning disable SKEXP0001, SKEXP0010, SKEXP0020, SKEXP0050

namespace KnowledgeEngine;

public class KnowledgeManager
{
    private readonly MongoVectorStore _vectorStore;
    private readonly IEmbeddingGenerator<string, Embedding<float>> _embeddingService;
    private readonly AtlasIndexManager _indexManager;
    private readonly KnowledgeSourceResolver _knowledgeSourceResolver;
    private readonly IMongoDatabase _mongoDatabase;

    public KnowledgeManager(
        MongoVectorStore vectorStore,
        IEmbeddingGenerator<string, Embedding<float>> embeddingService,
        AtlasIndexManager indexManager,
        IMongoDatabase mongoDatabase)
    {
        _vectorStore = vectorStore;
        _embeddingService = embeddingService;
        _indexManager = indexManager;
        _mongoDatabase = mongoDatabase;
        _knowledgeSourceResolver = new KnowledgeSourceResolver(new KnowledgeSourceFactory());
    }

    /// <summary>
    /// Saves a document to the vector store by parsing, chunking, and embedding it
    /// </summary>
    public async Task SaveToMemoryAsync(string documentPath, string collectionName)
    {
        var sw = Stopwatch.StartNew();
        LoggerProvider.Logger.Information(
            "⏫ Importing {File} into {Collection}",
            documentPath,
            collectionName
        );

        // 1. Parse the document
        KnowledgeParseResult parse;
        try
        {
            await using var fs = File.OpenRead(documentPath);
            parse = await _knowledgeSourceResolver.ParseAsync(fs, documentPath);
        }
        catch (Exception ex)
        {
            LoggerProvider.Logger.Error(ex, "Failed to read or parse the file {File}", documentPath);
            throw;
        }

        if (!parse.Success || parse.Document is null)
        {
            LoggerProvider.Logger.Warning("Document {File} is empty or invalid", documentPath);
            throw new InvalidOperationException($"Failed to parse {documentPath}: {parse.Error}");
        }

        // 2. Convert to text
        var doc = parse.Document;
        bool markdown = doc.Elements.Any(e => e is IHeadingElement);
        var rawText = markdown ? DocumentToTextConverter.Convert(doc) : doc.ToString();
        
        if (string.IsNullOrWhiteSpace(rawText))
        {
            LoggerProvider.Logger.Warning("Document {File} is empty or invalid", documentPath);
            throw new InvalidOperationException($"Failed to convert {documentPath} to text");
        }

        // 3. Chunk the text
        int maxLine = SettingsProvider.Settings.ChunkLineTokens > 0 
            ? SettingsProvider.Settings.ChunkLineTokens 
            : 60;
        int maxPara = SettingsProvider.Settings.ChunkParagraphTokens > 0 
            ? SettingsProvider.Settings.ChunkParagraphTokens 
            : 200;
        int overlap = Math.Max(0, SettingsProvider.Settings.ChunkOverlap);

        var lines = markdown
            ? TextChunker.SplitMarkDownLines(rawText, maxLine)
            : TextChunker.SplitPlainTextLines(rawText, maxLine);
        var paragraphs = markdown
            ? TextChunker.SplitMarkdownParagraphs(lines, maxPara, overlap)
            : TextChunker.SplitPlainTextParagraphs(lines, maxPara, overlap);

        // 4. Store chunks in vector store
        string source = doc.Source;
        string fileId = Path.GetFileNameWithoutExtension(documentPath);

        for (int i = 0; i < paragraphs.Count; i++)
        {
            var chunkOrder = i.ToString("D4");
            var chunkId = $"{fileId}-p{chunkOrder}";
            var chunkText = paragraphs[i];

            // Generate embedding for this chunk
            var embeddingResult = await _embeddingService.GenerateAsync(new[] { chunkText });
            var embedding = embeddingResult.First();

            // Store in vector store with metadata
            await UpsertAsync(collectionName, chunkId, chunkText, embedding);
        }

        LoggerProvider.Logger.Information(
            "✅ Stored {Count} chunks from {File} in {ElapsedMs} ms",
            paragraphs.Count,
            documentPath,
            sw.ElapsedMilliseconds
        );

        // 5. Create search index if needed
        try
        {
            await _indexManager.CreateIndexAsync(collectionName);
        }
        catch (Exception ex)
        {
            LoggerProvider.Logger.Warning(ex, "Index creation failed after import of {File}", documentPath);
        }
    }

    /// <summary>
    /// Upserts a chunk into the vector store
    /// </summary>
    private async Task UpsertAsync(
        string collectionName,
        string key,
        string text,
        Embedding<float> embedding,
        CancellationToken cancellationToken = default)
    {
        // Use MongoDB driver directly instead of vector store for now
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
            
            // Create a simple document for MongoDB storage
            var document = new MongoDB.Bson.BsonDocument
            {
                ["_id"] = key,
                ["text"] = text,
                ["vector"] = new MongoDB.Bson.BsonArray(embedding.Vector.ToArray()),
                ["source"] = source,
                ["chunkOrder"] = chunkOrder,
                ["tags"] = ""
            };

            // Get MongoDB collection directly
            var mongoCollection = _mongoDatabase.GetCollection<MongoDB.Bson.BsonDocument>(collectionName);
            
            // Use ReplaceOne with upsert option
            var filter = MongoDB.Driver.Builders<MongoDB.Bson.BsonDocument>.Filter.Eq("_id", key);
            var options = new ReplaceOptions { IsUpsert = true };
            
            await mongoCollection.ReplaceOneAsync(filter, document, options, cancellationToken);
            
            LoggerProvider.Logger.Information("Successfully upserted chunk {Key} to collection {Collection}", key, collectionName);
        }
        catch (Exception ex)
        {
            LoggerProvider.Logger.Error(ex, "Failed to upsert chunk {Key} to collection {Collection}", key, collectionName);
            throw;
        }
    }

    /// <summary>
    /// Searches the vector store for relevant chunks using MongoDB Atlas Vector Search
    /// </summary>
    public async Task<List<KnowledgeSearchResult>> SearchAsync(
        string collectionName,
        string query,
        int limit = 10,
        double minRelevanceScore = 0.6,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Generate embedding for the query
            var embeddingResult = await _embeddingService.GenerateAsync(new[] { query }, cancellationToken: cancellationToken);
            var queryEmbedding = embeddingResult.First();

            // Get MongoDB collection directly for vector search
            var mongoCollection = _mongoDatabase.GetCollection<MongoDB.Bson.BsonDocument>(collectionName);
            
            // Build MongoDB Atlas Vector Search aggregation pipeline
            var vectorSearchStage = new MongoDB.Bson.BsonDocument("$vectorSearch", new MongoDB.Bson.BsonDocument
            {
                ["index"] = SettingsProvider.Settings.Atlas.SearchIndexName,
                ["path"] = "vector",
                ["queryVector"] = new MongoDB.Bson.BsonArray(queryEmbedding.Vector.ToArray()),
                ["numCandidates"] = limit * 10, // Search more candidates for better results
                ["limit"] = limit
            });

            var projectStage = new MongoDB.Bson.BsonDocument("$project", new MongoDB.Bson.BsonDocument
            {
                ["_id"] = 1,
                ["text"] = 1,
                ["source"] = 1,
                ["chunkOrder"] = 1,
                ["tags"] = 1,
                ["score"] = new MongoDB.Bson.BsonDocument("$meta", "vectorSearchScore")
            });

            var pipeline = new[] { vectorSearchStage, projectStage };
            
            var searchResults = new List<KnowledgeSearchResult>();
            
            using var cursor = await mongoCollection.AggregateAsync<MongoDB.Bson.BsonDocument>(pipeline, cancellationToken: cancellationToken);
            
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
                "Vector search for query '{Query}' returned {Count} results above score {MinScore}",
                query, searchResults.Count, minRelevanceScore);

            return searchResults.OrderByDescending(r => r.Score).ToList();
        }
        catch (Exception ex)
        {
            LoggerProvider.Logger.Error(ex, "Failed to perform vector search for query: {Query}", query);
            return new List<KnowledgeSearchResult>();
        }
    }
}

/// <summary>
/// Represents a search result from the knowledge vector store
/// </summary>
public record KnowledgeSearchResult
{
    public string Text { get; init; } = string.Empty;
    public string Source { get; init; } = string.Empty;
    public int ChunkOrder { get; init; }
    public string Tags { get; init; } = string.Empty;
    public double Score { get; init; }
}
