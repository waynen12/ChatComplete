using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.Connectors.MongoDB;
using Microsoft.SemanticKernel.Memory;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.KernelMemory;
using MongoDB.Driver;
using ChatCompletion;
using Microsoft.SemanticKernel.Text;   // NEW
using ChatCompletion.Config;           // to reach SettingsProvider
using MongoDB.Driver.Core.Authentication;


#pragma warning disable SKEXP0001, SKEXP0010, SKEXP0020, SKEXP0050

public class KnowledgeManager
{
    private readonly AtlasIndexManager _indexManager;
    ISemanticTextMemory _memory;

    private KnowledgeSourceResolver _knowledgeSourceResolver;

    public KnowledgeManager(ISemanticTextMemory memory, AtlasIndexManager indexManager)
    {
        _memory = memory;
        _indexManager = indexManager;
        _knowledgeSourceResolver = new KnowledgeSourceResolver(new KnowledgeSourceFactory());
    }

       // to reach SettingsProvider
// … other usings …

    public async Task<ISemanticTextMemory> SaveToMemoryAsync(string documentPath, string collectionName)
    {
        LoggerProvider.Logger.Information(
            "Importing {File} into collection {Collection}",
            documentPath, collectionName);

        // ─── 1. Parse the document ────────────────────────────────────────────────
        KnowledgeParseResult parseResult;
        await using (var fs = new FileStream(documentPath, FileMode.Open, FileAccess.Read))
        {
            parseResult = await _knowledgeSourceResolver.ParseAsync(fs, documentPath);
        }

        if (!parseResult.Success || parseResult.Document is null)
        {
            LoggerProvider.Logger.Error(
                "Failed to parse {File}: {Err}", documentPath, parseResult.Error);
            throw new InvalidOperationException(
                $"Failed to parse {documentPath}: {parseResult.Error}");
        }

        var doc          = parseResult.Document;
        bool isStructured = doc.Elements.Any(e => e is IHeadingElement);

        // ─── 2. Convert to raw text (markdown if structured) ──────────────────────
        string? rawText = isStructured
            ? DocumentToTextConverter.Convert(doc)   // headings preserved as "## …"
            : doc.ToString();

        // Chunking parameters from configuration (with safe fall-backs)
        int lineTokens       = SettingsProvider.Settings.ChunkLineTokens      > 0
                            ? SettingsProvider.Settings.ChunkLineTokens      : 60;
        int paragraphTokens  = SettingsProvider.Settings.ChunkParagraphTokens > 0
                            ? SettingsProvider.Settings.ChunkParagraphTokens : 200;
        int overlapTokens    = SettingsProvider.Settings.ChunkOverlap         >= 0
                            ? SettingsProvider.Settings.ChunkOverlap         : 0;

        if (string.IsNullOrWhiteSpace(rawText))
        {
            LoggerProvider.Logger.Error(
                "Failed to convert {File} to text", documentPath);
            throw new InvalidOperationException(
                $"Failed to convert {documentPath} to text");
        }                   

        // ─── 3. Token-aware chunking ──────────────────────────────────────────────
        List<string> lines = isStructured
            ? TextChunker.SplitMarkDownLines(rawText, lineTokens)
            : TextChunker.SplitPlainTextLines(rawText, lineTokens);

        List<string> paragraphs = isStructured
            ? TextChunker.SplitMarkdownParagraphs(lines, paragraphTokens, overlapTokens)
            : TextChunker.SplitPlainTextParagraphs(lines, paragraphTokens, overlapTokens);

        // ─── 4. Persist chunks to the vector store ────────────────────────────────
        int chunkIndex = 0;
        foreach (var paragraph in paragraphs)
        {
            var chunk = new KnowledgeChunk
            {
                Content = paragraph,
                Metadata = new KnowledgeMetadata
                {
                    Source  = doc.Source,
                    Section = $"chunk-{chunkIndex++}",
                    Tags    = Array.Empty<string>() // add your own tag logic if desired
                }
            };

            await _memory.SaveReferenceAsync(
                collection:            collectionName,
                description:           chunk.Content,
                text:                  chunk.Content,
                externalId:            chunk.Metadata.Section,
                externalSourceName:    chunk.Metadata.Source,
                additionalMetadata:    string.Empty);
        }

        LoggerProvider.Logger.Information(
            "Stored {Count} chunks from {File}", paragraphs.Count, documentPath);

        // ─── 5. Ensure a vector index exists ──────────────────────────────────────
        await _indexManager.CreateIndexAsync();

        return _memory;
    }





    public async Task CreateIndexAsync()
    {
        await _indexManager.CreateIndexAsync();
    }

   

    public async Task<ISemanticTextMemory> SaveKnowledgeDocumentsToMemory(List<KnowledgeChunk> chunks, string sourceName, string collectionName)
    {

        foreach (var chunk in chunks)
        {
            await _memory.SaveReferenceAsync(
                collection: collectionName,
                description: chunk.Content,
                text: chunk.Content,
                externalId: chunk.Metadata.Section,
                externalSourceName: chunk.Metadata.Source,
                additionalMetadata: string.Join(", ", chunk.Metadata.Tags)
            );
            Console.WriteLine($"✅ Saved chunk: {chunk.Metadata.Section} ({chunk.Content.Length} chars)");
        }

        Console.WriteLine("✅ All chunks imported successfully.");
        return _memory;
    }
}