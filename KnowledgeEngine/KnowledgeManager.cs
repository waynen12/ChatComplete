using System.Diagnostics;
using ChatCompletion.Config;
using KnowledgeEngine.Logging;
using KnowledgeEngine.Models;
using KnowledgeEngine.Persistence.IndexManagers;
using KnowledgeEngine.Persistence.VectorStores;
using Microsoft.Extensions.AI;
using Microsoft.SemanticKernel.Text;

#pragma warning disable SKEXP0001, SKEXP0010, SKEXP0020, SKEXP0050

namespace KnowledgeEngine;

public class KnowledgeManager
{
    private readonly IVectorStoreStrategy _vectorStoreStrategy;
    private readonly IEmbeddingGenerator<string, Embedding<float>> _embeddingService;
    private readonly IIndexManager _indexManager;
    private readonly KnowledgeSourceResolver _knowledgeSourceResolver;

    public KnowledgeManager(
        IVectorStoreStrategy vectorStoreStrategy,
        IEmbeddingGenerator<string, Embedding<float>> embeddingService,
        IIndexManager indexManager)
    {
        _vectorStoreStrategy = vectorStoreStrategy;
        _embeddingService = embeddingService;
        _indexManager = indexManager;
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
        
        LoggerProvider.Logger.Information("Document processing: {HeadingCount} headings found, markdown={Markdown}", 
            doc.Elements.OfType<IHeadingElement>().Count(), markdown);
            
        var rawText = markdown ? DocumentToTextConverter.Convert(doc, SettingsProvider.Settings) : doc.ToString();
        
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
            await _vectorStoreStrategy.UpsertAsync(collectionName, chunkId, chunkText, embedding);
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
    /// Searches the vector store for relevant chunks using vector search
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

            // Use the vector store strategy for search
            var searchResults = await _vectorStoreStrategy.SearchAsync(
                collectionName, 
                query, 
                queryEmbedding, 
                limit, 
                minRelevanceScore, 
                cancellationToken);

            LoggerProvider.Logger.Information(
                "Vector search for query '{Query}' returned {Count} results above score {MinScore}",
                query, searchResults.Count, minRelevanceScore);

            return searchResults;
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
