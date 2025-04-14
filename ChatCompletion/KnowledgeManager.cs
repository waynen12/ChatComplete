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
    MemoryBuilder _memoryBuilder; 
    string _openAiApiKey;
    const string TextEmbeddingModelName = "text-embedding-ada-002";

    ISemanticTextMemory _memory;

    public KnowledgeManager(ISemanticTextMemory memory)
    {
        _memory = memory;
    }

    public async Task<ISemanticTextMemory> SaveKnowledgeDocumentsToMemory(
    string documentPath,
    string sourceName,
    string collectionName)
    {
        Console.WriteLine($"Importing chunks from '{documentPath}' into collection '{collectionName}'...");

        var markdown = File.ReadAllText(documentPath);
        var chunks = KnowledgeChunker.ChunkFromMarkdown(markdown, sourceName);

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