using Xunit;

namespace KnowledgeManager.Tests.KnowledgeEngine;

public class KnowledgeChunkerTests
{
    [Fact]
    public void ChunkFromMarkdown_ShouldSplitOnH1H2H3Boundaries()
    {
        var markdown =
            """
            # Root

            Intro content.

            ## Details

            Detail text.

            ### Deep Dive

            Deep dive text.
            """;

        var chunks = KnowledgeChunker.ChunkFromMarkdown(markdown, "source.md", tokenLimit: 100);

        Assert.Equal(3, chunks.Count);
        Assert.Equal("Root", chunks[0].Metadata.Section);
        Assert.Equal("Details", chunks[1].Metadata.Section);
        Assert.Equal("Deep Dive", chunks[2].Metadata.Section);
        Assert.All(chunks, chunk => Assert.Equal("source.md", chunk.Metadata.Source));
    }

    [Fact]
    public void ChunkFromMarkdown_ShouldSubChunkLargeSectionByTokenLimit()
    {
        var paragraph1 = string.Join(" ", Enumerable.Range(1, 11).Select(i => $"alpha{i}"));
        var paragraph2 = string.Join(" ", Enumerable.Range(1, 11).Select(i => $"beta{i}"));
        var markdown =
            $"""
            ## Big Section

            {paragraph1}

            {paragraph2}
            """;

        var chunks = KnowledgeChunker.ChunkFromMarkdown(markdown, "big.md", tokenLimit: 12);

        Assert.True(chunks.Count >= 2);
        Assert.All(chunks, chunk => Assert.Equal("Big Section", chunk.Metadata.Section));
        Assert.All(chunks, chunk => Assert.StartsWith("## Big Section", chunk.Content));
    }

    [Fact]
    public void ChunkFromMarkdown_ShouldSplitOversizedParagraphIntoMultipleChunks()
    {
        var oversizedParagraph = string.Join(" ", Enumerable.Range(1, 25).Select(i => $"word{i}"));
        var markdown =
            $"""
            ## Oversized

            {oversizedParagraph}
            """;

        var chunks = KnowledgeChunker.ChunkFromMarkdown(markdown, "oversized.md", tokenLimit: 10);

        Assert.True(chunks.Count >= 3);
        Assert.All(chunks, chunk => Assert.Equal("Oversized", chunk.Metadata.Section));
        Assert.All(chunks, chunk => Assert.StartsWith("## Oversized", chunk.Content));
    }

    [Fact]
    public void ChunkFromMarkdown_ShouldIncludeSectionTagInMetadata()
    {
        var markdown =
            """
            ## API Reference: v1

            Endpoint details.
            """;

        var chunks = KnowledgeChunker.ChunkFromMarkdown(markdown, "api.md", tokenLimit: 100);

        Assert.Single(chunks);
        Assert.Contains("section-api-reference-v1", chunks[0].Metadata.Tags);
    }

}
