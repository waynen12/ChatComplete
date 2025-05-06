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

#pragma warning disable SKEXP0001, SKEXP0010, SKEXP0020, SKEXP0050

public class KnowledgeManager
{
    private readonly AtlasIndexManager _indexManager;
    const string TextEmbeddingModelName = "text-embedding-ada-002";

    ISemanticTextMemory _memory;

    public KnowledgeManager(ISemanticTextMemory memory, AtlasIndexManager indexManager)
    {
        _memory = memory;
        _indexManager = indexManager;
    }

    public async Task<ISemanticTextMemory> SaveMarkDownToMemory(
    string documentPath,
    string sourceName,
    string collectionName)
    {
        Console.WriteLine($"Importing chunks from '{documentPath}' into collection '{collectionName}'...");

        var markdown = File.ReadAllText(documentPath);
        var markdownChunks = KnowledgeChunker.ChunkFromMarkdown(markdown, sourceName);
        ISemanticTextMemory memory = await SaveKnowledgeDocumentsToMemory(markdownChunks, sourceName, collectionName);
        return memory;

    }

    public async Task CreateIndexAsync(string collectionName)
    {
        await _indexManager.CreateIndexAsync(collectionName);
    }

    public async Task<ISemanticTextMemory> SaveDocxToMemory(
        string documentPath,
        string sourceName,
        string collectionName)
    {
        ISemanticTextMemory memory;
        Console.WriteLine($"Importing chunks from '{documentPath}' into collection '{collectionName}'...");

        DocxToDocumentConverter docxToDocumentConverter = new DocxToDocumentConverter();
        DocxKnowledgeSource docxKnowledgeSource = new DocxKnowledgeSource();
        KnowledgeParseResult result;

        await using (var fileStream = new FileStream(documentPath, FileMode.Open, FileAccess.Read)) // Use 'using'
        {
            result = await docxKnowledgeSource.ParseAsync(fileStream);
        }
        if (result == null)
        {
            throw new Exception("SaveDocxToMemory -> Parse result is null");
        }
        var document = result?.Document;
        if (document == null)
        {
            throw new Exception("SaveDocxToMemory -> Parsed Document is null");
        }
        var isStructured = document.Elements.Any(e => e is IHeadingElement);
        if (isStructured)
        {
            var markdown = DocumentToTextConverter.Convert(document);
            var markdownChunks = KnowledgeChunker.ChunkFromMarkdown(markdown, sourceName);
            memory = await SaveKnowledgeDocumentsToMemory(markdownChunks, sourceName, collectionName);
            await _indexManager.CreateIndexAsync(collectionName);

        }
        else
        {
            var textChunks= KnowledgeChunker.ChunkFromPlainText(document?.ToString(), sourceName);
            memory =  await SaveKnowledgeDocumentsToMemory(textChunks, sourceName, collectionName);
            await _indexManager.CreateIndexAsync(collectionName);

        }
        return memory;
        
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