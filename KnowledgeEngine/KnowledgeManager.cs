using System.Diagnostics;
using ChatCompletion.Config;
using KnowledgeEngine.Logging;
using KnowledgeEngine.Models;
using KnowledgeEngine.Persistence;
using KnowledgeEngine.Persistence.IndexManagers;
using KnowledgeEngine.Persistence.VectorStores;
using Microsoft.Extensions.AI;

namespace KnowledgeEngine;

public class KnowledgeManager
{
    private readonly IVectorStoreStrategy _vectorStoreStrategy;
    private readonly IEmbeddingGenerator<string, Embedding<float>> _embeddingService;
    private readonly IIndexManager _indexManager;
    private readonly IKnowledgeRepository _knowledgeRepository;
    private readonly KnowledgeSourceResolver _knowledgeSourceResolver;

    public KnowledgeManager(
        IVectorStoreStrategy vectorStoreStrategy,
        IEmbeddingGenerator<string, Embedding<float>> embeddingService,
        IIndexManager indexManager,
        IKnowledgeRepository knowledgeRepository
    )
    {
        _vectorStoreStrategy = vectorStoreStrategy;
        _embeddingService = embeddingService;
        _indexManager = indexManager;
        _knowledgeRepository = knowledgeRepository;
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
            LoggerProvider.Logger.Error(
                ex,
                "Failed to read or parse the file {File}",
                documentPath
            );
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

        LoggerProvider.Logger.Information(
            "Document processing: {HeadingCount} headings found, markdown={Markdown}",
            doc.Elements.OfType<IHeadingElement>().Count(),
            markdown
        );

        var rawText = markdown
            ? DocumentToTextConverter.Convert(doc, SettingsProvider.Settings)
            : doc.ToString();

        if (string.IsNullOrWhiteSpace(rawText))
        {
            LoggerProvider.Logger.Warning("Document {File} is empty or invalid", documentPath);
            throw new InvalidOperationException($"Failed to convert {documentPath} to text");
        }

        // 3. Chunk the text using markdown-aware chunker
        string source = doc.Source;
        string fileId = Path.GetFileNameWithoutExtension(documentPath);

        List<KnowledgeChunk> chunks = markdown
            ? KnowledgeChunker.ChunkFromMarkdown(rawText, source)
            : KnowledgeChunker.ChunkFromPlainText(rawText, source);

        LoggerProvider.Logger.Information(
            "📝 Chunked {File} into {Count} chunks (markdown={Markdown})",
            documentPath, chunks.Count, markdown
        );

        // 4. Generate embeddings and store chunks in vector store
        for (int i = 0; i < chunks.Count; i++)
        {
            var chunk = chunks[i];
            var chunkOrder = i.ToString("D4");
            var chunkId = $"{fileId}-p{chunkOrder}";

            // Generate embedding for this chunk
            var embeddingResult = await _embeddingService.GenerateAsync(
                new[] { chunk.Content }
            );
            var embedding = embeddingResult.First();

            // Store in vector store with metadata
            await _vectorStoreStrategy.UpsertAsync(collectionName, chunkId, chunk.Content, embedding);
        }

        LoggerProvider.Logger.Information(
            "✅ Stored {Count} chunks from {File} in {ElapsedMs} ms",
            chunks.Count,
            documentPath,
            sw.ElapsedMilliseconds
        );

        // 5. Record collection metadata in SQLite
        try
        {
            // Create or update collection record
            await _knowledgeRepository.CreateOrUpdateCollectionAsync(
                collectionName,
                collectionName, // Use collection name as display name
                $"Knowledge collection created from {Path.GetFileName(documentPath)}"
            );

            // Record individual document metadata
            var fileName = Path.GetFileName(documentPath);
            var fileInfo = new FileInfo(documentPath);
            var fileSize = fileInfo.Exists ? fileInfo.Length : 0;
            var fileType = Path.GetExtension(documentPath).TrimStart('.').ToLowerInvariant();

            // Generate document ID from filename
            var documentId = fileId; // Already computed above

            await _knowledgeRepository.AddDocumentAsync(
                collectionName,
                documentId,
                fileName,
                fileSize,
                fileType
            );

            // Update document and chunk counts
            await _knowledgeRepository.UpdateCollectionStatsAsync(
                collectionName,
                1, // documentCount - this is one document
                chunks.Count // chunkCount
            );

            LoggerProvider.Logger.Information(
                "📊 Updated collection metadata for {Collection}: 1 document ({FileName}), {ChunkCount} chunks",
                collectionName,
                fileName,
                chunks.Count
            );
        }
        catch (Exception ex)
        {
            LoggerProvider.Logger.Warning(
                ex,
                "Failed to record collection metadata for {Collection}",
                collectionName
            );
        }

        // 6. Create search index if needed
        try
        {
            await _indexManager.CreateIndexAsync(collectionName);
        }
        catch (Exception ex)
        {
            LoggerProvider.Logger.Warning(
                ex,
                "Index creation failed after import of {File}",
                documentPath
            );
        }
    }

    /// <summary>
    /// Searches the vector store for relevant chunks using vector search
    /// </summary>
    public async Task<List<KnowledgeSearchResult>> SearchAsync(
        string collectionName,
        string query,
        int limit = 10,
        double minRelevanceScore = 0.3, // Lowered from 0.6 for Ollama embeddings
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            LoggerProvider.Logger.Information(
                "🔍 KnowledgeManager.SearchAsync started - Collection: '{Collection}', Query: '{Query}', Limit: {Limit}, MinScore: {MinScore}",
                collectionName,
                query.Length > 50 ? query.Substring(0, 50) + "..." : query,
                limit,
                minRelevanceScore
            );

            // Generate embedding for the query
            LoggerProvider.Logger.Information("📊 Generating embedding for search query using {EmbeddingService}", _embeddingService.GetType().Name);
            
            var embeddingResult = await _embeddingService.GenerateAsync(
                new[] { query },
                cancellationToken: cancellationToken
            );
            var queryEmbedding = embeddingResult.First();
            
            LoggerProvider.Logger.Information(
                "✅ Embedding generated successfully - Vector size: {VectorSize} dimensions",
                queryEmbedding.Vector.Length
            );

            // Use the vector store strategy for search
            LoggerProvider.Logger.Information("🔍 Calling vector store search with strategy: {StrategyType}", _vectorStoreStrategy.GetType().Name);
            
            var searchResults = await _vectorStoreStrategy.SearchAsync(
                collectionName,
                query,
                queryEmbedding,
                limit,
                minRelevanceScore,
                cancellationToken
            );

            LoggerProvider.Logger.Information(
                "📋 Vector search for query '{Query}' returned {Count} results above score {MinScore}",
                query.Length > 30 ? query.Substring(0, 30) + "..." : query,
                searchResults.Count,
                minRelevanceScore
            );

            if (searchResults.Count == 0)
            {
                LoggerProvider.Logger.Warning(
                    "⚠️ No search results returned - this could indicate: 1) Collection doesn't exist, 2) No documents match threshold, 3) Embedding dimension mismatch"
                );
            }

            return searchResults;
        }
        catch (Exception ex)
        {
            LoggerProvider.Logger.Error(
                ex,
                "❌ Failed to perform vector search for query: '{Query}' in collection: '{Collection}'",
                query,
                collectionName
            );
            return new List<KnowledgeSearchResult>();
        }
    }

    /// <summary>
    /// Gets all available knowledge collections
    /// </summary>
    public async Task<List<string>> GetAvailableCollectionsAsync(
        CancellationToken cancellationToken = default
    )
    {
        return await _vectorStoreStrategy.ListCollectionsAsync(cancellationToken);
    }
}
