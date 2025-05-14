using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.Connectors.MongoDB;
using Microsoft.SemanticKernel.Memory;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.KernelMemory;
using MongoDB.Driver;
using ChatCompletion;
using MongoDB.Driver.Core.Authentication;
using Microsoft.Extensions.Logging;

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

    public async Task<ISemanticTextMemory> SaveToMemoryAsync(string documentPath, string collectionName)
{
    Console.WriteLine($"Importing chunks from '{documentPath}' into collection '{collectionName}'...");

    KnowledgeParseResult result;
    await using (var fileStream = new FileStream(documentPath, FileMode.Open, FileAccess.Read))
    {
        result = await _knowledgeSourceResolver.ParseAsync(fileStream, documentPath);
    }

    if (!result.Success || result.Document == null)
    {
        LoggerProvider.Logger.Error($"Failed to parse document '{documentPath}': {result.Error}");
        throw new Exception($"Failed to parse document '{documentPath}': {result.Error}");
    }

    var document = result.Document;
    var isStructured = document.Elements.Any(e => e is IHeadingElement);

    List<KnowledgeChunk> chunks;
    if (isStructured)
    {
        var markdown = DocumentToTextConverter.Convert(document);
        chunks = KnowledgeChunker.ChunkFromMarkdown(markdown, document.Source);
    }
    else
    {
        chunks = KnowledgeChunker.ChunkFromPlainText(document.ToString(), document.Source);
    }

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
    LoggerProvider.Logger.Information("All chunks imported successfully.");

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