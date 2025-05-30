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
using System.Diagnostics;
using Serilog;


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
        var sw = Stopwatch.StartNew();
        LoggerProvider.Logger.Information("⏫ Importing {File} into {Collection}", documentPath, collectionName);

        // 1. Parse
        KnowledgeParseResult parse;
        try
        {
            await using (var fs = File.OpenRead(documentPath))
            {
                parse = await _knowledgeSourceResolver.ParseAsync(fs, documentPath);
            }
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

        // 2. Convert
            var doc = parse.Document;
        bool markdown = doc.Elements.Any(e => e is IHeadingElement);
        var rawText = markdown ? DocumentToTextConverter.Convert(doc) : doc.ToString();
        if (string.IsNullOrWhiteSpace(rawText))
        {
            LoggerProvider.Logger.Warning("Document {File} is empty or invalid", documentPath);
            throw new InvalidOperationException($"Failed to convert {documentPath} to text");
        }

        // 3. Chunk
        int maxLine = SettingsProvider.Settings.ChunkLineTokens > 0 ? SettingsProvider.Settings.ChunkLineTokens : 60;
        int maxPara = SettingsProvider.Settings.ChunkParagraphTokens > 0 ? SettingsProvider.Settings.ChunkParagraphTokens : 200;
        int overlap = Math.Max(0, SettingsProvider.Settings.ChunkOverlap);

        var lines = markdown ? TextChunker.SplitMarkDownLines(rawText, maxLine)
                                : TextChunker.SplitPlainTextLines(rawText, maxLine);
        var paragraphs = markdown ? TextChunker.SplitMarkdownParagraphs(lines, maxPara, overlap)
                                : TextChunker.SplitPlainTextParagraphs(lines, maxPara, overlap);

        // 4. Persist
        string source = doc.Source;
        string fileId = Path.GetFileNameWithoutExtension(documentPath);

        for (int i = 0; i < paragraphs.Count; i++)
        {
            await _memory.SaveReferenceAsync(
                collection: collectionName,
                description: paragraphs[i],
                text: paragraphs[i],
                externalId: $"{fileId}-p{i:0000}",
                externalSourceName: source,
                additionalMetadata: string.Empty);
        }

        LoggerProvider.Logger.Information(
            "✅ Stored {Cnt} chunks from {File} in {Ms} ms",
            paragraphs.Count, documentPath, sw.ElapsedMilliseconds);

        try { await _indexManager.CreateIndexAsync(); }
        catch (Exception ex)
        {
            LoggerProvider.Logger.Warning(ex, "Index creation failed after import of {File}", documentPath);
        }

        return _memory;
    }






    public async Task CreateIndexAsync()
    {
        await _indexManager.CreateIndexAsync();
    }   
    
     
}