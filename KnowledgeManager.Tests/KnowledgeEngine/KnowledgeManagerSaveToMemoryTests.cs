using ChatCompletion.Config;
using Knowledge.Contracts;
using KnowledgeEngine;
using KnowledgeEngine.Persistence;
using KnowledgeEngine.Persistence.IndexManagers;
using KnowledgeEngine.Persistence.VectorStores;
using Microsoft.Extensions.AI;
using Moq;

namespace KnowledgeManager.Tests.KnowledgeEngine;

public class KnowledgeManagerSaveToMemoryTests : IDisposable
{
    private readonly Mock<IVectorStoreStrategy> _vectorStoreStrategy = new();
    private readonly Mock<IEmbeddingGenerator<string, Embedding<float>>> _embeddingService = new();
    private readonly Mock<IIndexManager> _indexManager = new();
    private readonly Mock<IKnowledgeRepository> _knowledgeRepository = new();
    private readonly string _tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));

    public KnowledgeManagerSaveToMemoryTests()
    {
        Directory.CreateDirectory(_tempDirectory);
        SettingsProvider.Initialize(new ChatCompleteSettings
        {
            ChunkParagraphTokens = 8,
            ChunkCharacterLimit = 4096
        });

        _knowledgeRepository
            .Setup(repository => repository.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<KnowledgeSummaryDto>());
        _knowledgeRepository
            .Setup(repository => repository.ExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _knowledgeRepository
            .Setup(repository => repository.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _knowledgeRepository
            .Setup(repository => repository.CreateOrUpdateCollectionAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("collection-id");
        _knowledgeRepository
            .Setup(repository => repository.UpdateCollectionStatsAsync(
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _knowledgeRepository
            .Setup(repository => repository.GetDocumentsByCollectionAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<DocumentMetadataDto>());
        _knowledgeRepository
            .Setup(repository => repository.GetDocumentChunksAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<DocumentChunkDto>());
        _knowledgeRepository
            .Setup(repository => repository.AddDocumentAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<long>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("document-id");

        _indexManager
            .Setup(manager => manager.CreateIndexAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    [Fact]
    public async Task SaveToMemoryAsync_UsesSingleBatchEmbeddingCall_AndPassesChunkMetadata()
    {
        var documentPath = Path.Combine(_tempDirectory, "guide.md");
        var markdown = """
            Intro text before any heading.

            ## Features

            First feature paragraph with several words.

            Second feature paragraph with several more words to trigger another chunk.
            """;
        await File.WriteAllTextAsync(documentPath, markdown);

        var expectedChunks = KnowledgeChunker.ChunkFromMarkdown(
            markdown,
            "guide.md",
            SettingsProvider.Settings
        );

        List<string>? embeddedInputs = null;
        var embeddings = expectedChunks
            .Select((_, index) => CreateEmbedding(index + 1))
            .ToArray();

        _embeddingService
            .Setup(service => service.GenerateAsync(
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<EmbeddingGenerationOptions?>(),
                It.IsAny<CancellationToken>()))
            .Returns((IEnumerable<string> values, EmbeddingGenerationOptions? _, CancellationToken _) =>
            {
                embeddedInputs = values.ToList();
                return Task.FromResult(new GeneratedEmbeddings<Embedding<float>>(embeddings));
            });

        var upserts = new List<(string id, string text, Embedding<float> embedding, string source, int order, string section, string tags)>();
        _vectorStoreStrategy
            .Setup(strategy => strategy.UpsertAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<Embedding<float>>(),
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask)
            .Callback((string collection, string id, string text, Embedding<float> embedding, string source, int order, string section, string tags, CancellationToken _) =>
            {
                upserts.Add((id, text, embedding, source, order, section, tags));
            });

        var sut = new global::KnowledgeEngine.KnowledgeManager(
            _vectorStoreStrategy.Object,
            _embeddingService.Object,
            _indexManager.Object,
            _knowledgeRepository.Object
        );

        await sut.SaveToMemoryAsync(documentPath, "knowledge");

        _embeddingService.Verify(service => service.GenerateAsync(
            It.IsAny<IEnumerable<string>>(),
            It.IsAny<EmbeddingGenerationOptions?>(),
            It.IsAny<CancellationToken>()), Times.Once);
        Assert.NotNull(embeddedInputs);
        Assert.Equal(upserts.Count, embeddedInputs!.Count);
        Assert.Equal(expectedChunks.Count, upserts.Count);

        var orderedUpserts = upserts.OrderBy(item => item.order).ToList();
        for (int i = 0; i < expectedChunks.Count; i++)
        {
            var expectedChunk = expectedChunks[i];
            var upsert = orderedUpserts[i];

            Assert.Equal($"guide-p{i:D4}", upsert.id);
            Assert.Equal("guide.md", upsert.source);
            Assert.Equal(i, upsert.order);
            Assert.Equal(expectedChunk.Content, upsert.text);
            Assert.Equal(expectedChunk.Metadata.Section, upsert.section);
            Assert.Equal(string.Join(',', expectedChunk.Metadata.Tags), upsert.tags);
            Assert.Same(embeddings[i], upsert.embedding);
        }

        Assert.Equal("Preamble", orderedUpserts[0].section);
        Assert.Contains(orderedUpserts.Skip(1), item => item.section == "Features");
        Assert.All(orderedUpserts.Skip(1), item => Assert.StartsWith("## Features", item.text, StringComparison.Ordinal));
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
    }

    private static Embedding<float> CreateEmbedding(int index)
    {
        var value = (float)index;
        return new Embedding<float>(new[] { value, value + 1, value + 2 });
    }
}
