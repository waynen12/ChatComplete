using System.Text;
using ChatCompletion.Config;
using KnowledgeEngine.Models;
using KnowledgeEngine.Persistence;
using KnowledgeEngine.Persistence.IndexManagers;
using KnowledgeEngine.Persistence.VectorStores;
using Microsoft.Extensions.AI;
using Moq;
using Xunit;

namespace KnowledgeManager.Tests.KnowledgeEngine;

public class KnowledgeManagerSaveToMemoryTests
{
    [Fact]
    public async Task SaveToMemoryAsync_ShouldUpsertWithSectionMetadata_AndGenerateEmbeddingPerChunk()
    {
        SettingsProvider.Initialize(
            new ChatCompleteSettings
            {
                ChunkParagraphTokens = 50,
                ChunkCharacterLimit = 4096
            }
        );

        var vectorStore = new Mock<IVectorStoreStrategy>();
        var indexManager = new Mock<IIndexManager>();
        var repository = new Mock<IKnowledgeRepository>();
        var embedding = new Mock<IEmbeddingGenerator<string, Embedding<float>>>();

        embedding
            .Setup(e => e.GenerateAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<EmbeddingGenerationOptions?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                new GeneratedEmbeddings<Embedding<float>>(
                    new[] { new Embedding<float>(new[] { 0.1f, 0.2f, 0.3f }) }
                )
            );

        var capturedUpserts = new List<(string text, string source, string section, string[] tags)>();
        vectorStore
            .Setup(v =>
                v.UpsertAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<Embedding<float>>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string[]>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .Callback<
                string,
                string,
                string,
                Embedding<float>,
                string,
                string,
                string[],
                CancellationToken
            >((_, _, text, _, source, section, tags, _) =>
            {
                capturedUpserts.Add((text, source, section, tags));
            })
            .Returns(Task.CompletedTask);

        var manager = new global::KnowledgeEngine.KnowledgeManager(
            vectorStore.Object,
            embedding.Object,
            indexManager.Object,
            repository.Object
        );

        var tempFile = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.md");
        await File.WriteAllTextAsync(
            tempFile,
            """
            # Sample Title

            Intro paragraph.

            ## Architecture

            System architecture section.

            ### Components

            Component details.
            """,
            Encoding.UTF8
        );

        try
        {
            await manager.SaveToMemoryAsync(tempFile, "test-collection");
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }

        Assert.True(capturedUpserts.Count >= 3);
        Assert.Contains(capturedUpserts, upsert => upsert.section == "Sample Title");
        Assert.Contains(capturedUpserts, upsert => upsert.section == "Architecture");
        Assert.Contains(capturedUpserts, upsert => upsert.section == "Components");
        Assert.All(capturedUpserts, upsert => Assert.EndsWith(".md", upsert.source));
        Assert.All(
            capturedUpserts,
            upsert => Assert.Contains(upsert.tags, tag => tag.StartsWith("section-", StringComparison.Ordinal))
        );

        embedding.Verify(
            e => e.GenerateAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<EmbeddingGenerationOptions?>(), It.IsAny<CancellationToken>()),
            Times.Exactly(capturedUpserts.Count)
        );
    }
}
