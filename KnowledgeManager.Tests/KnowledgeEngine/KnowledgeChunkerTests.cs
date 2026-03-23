using ChatCompletion.Config;
using KnowledgeEngine;

namespace KnowledgeManager.Tests.KnowledgeEngine;

public class KnowledgeChunkerTests
{
    private static readonly ChatCompleteSettings TestSettings = new()
    {
        ChunkParagraphTokens = 12,
        ChunkCharacterLimit = 4096
    };

    [Fact]
    public void ChunkFromMarkdown_SplitsOnHeadingBoundaries_AndPreservesMetadata()
    {
        var markdown = """
            # Overview

            Intro paragraph.

            ## Details

            Details paragraph.

            ### Deep Dive

            Deep dive paragraph.
            """;

        var chunks = KnowledgeChunker.ChunkFromMarkdown(markdown, "guide.md", TestSettings);

        Assert.Equal(3, chunks.Count);
        Assert.Collection(
            chunks,
            chunk =>
            {
                Assert.Equal("guide.md", chunk.Metadata.Source);
                Assert.Equal("Overview", chunk.Metadata.Section);
                Assert.StartsWith("# Overview", chunk.Content, StringComparison.Ordinal);
            },
            chunk =>
            {
                Assert.Equal("guide.md", chunk.Metadata.Source);
                Assert.Equal("Details", chunk.Metadata.Section);
                Assert.StartsWith("## Details", chunk.Content, StringComparison.Ordinal);
            },
            chunk =>
            {
                Assert.Equal("guide.md", chunk.Metadata.Source);
                Assert.Equal("Deep Dive", chunk.Metadata.Section);
                Assert.StartsWith("### Deep Dive", chunk.Content, StringComparison.Ordinal);
            }
        );
    }

    [Fact]
    public void ChunkFromMarkdown_KeepsRelatedParagraphsTogether_WhenUnderTokenLimit()
    {
        var markdown = """
            ## Features

            First short paragraph with a few words.

            Second short paragraph stays with the first.
            """;

        var chunks = KnowledgeChunker.ChunkFromMarkdown(markdown, "features.md", new ChatCompleteSettings
        {
            ChunkParagraphTokens = 30
        });

        var chunk = Assert.Single(chunks);
        Assert.Contains("First short paragraph", chunk.Content, StringComparison.Ordinal);
        Assert.Contains("Second short paragraph", chunk.Content, StringComparison.Ordinal);
        Assert.Equal("Features", chunk.Metadata.Section);
    }

    [Fact]
    public void ChunkFromMarkdown_SubChunksOversizedSection_WhileKeepingHeadingContext()
    {
        var markdown = """
            ## Large Section

            alpha beta gamma delta epsilon zeta eta theta iota kappa lambda mu nu xi omicron pi rho sigma tau.
            """;

        var chunks = KnowledgeChunker.ChunkFromMarkdown(markdown, "large.md", TestSettings);

        Assert.True(chunks.Count > 1);
        Assert.All(chunks, chunk =>
        {
            Assert.Equal("Large Section", chunk.Metadata.Section);
            Assert.StartsWith("## Large Section", chunk.Content, StringComparison.Ordinal);
            Assert.Equal("large.md", chunk.Metadata.Source);
        });
    }

    [Fact]
    public void ChunkFromPlainText_SubChunksOversizedContent_WithFallbackMetadata()
    {
        var text =
            "alpha beta gamma delta epsilon zeta eta theta iota kappa lambda mu nu xi omicron";

        var chunks = KnowledgeChunker.ChunkFromPlainText(text, "notes.txt", TestSettings);

        Assert.True(chunks.Count > 1);
        Assert.All(chunks, chunk =>
        {
            Assert.Equal("notes.txt", chunk.Metadata.Source);
            Assert.Equal("Document", chunk.Metadata.Section);
            Assert.DoesNotContain('#', chunk.Content);
        });
    }
}
