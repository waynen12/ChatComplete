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
    public void ChunkFromMarkdown_PreservesPreambleBeforeFirstHeading()
    {
        var markdown = """
            Intro text before any heading.

            Still in the preamble.

            # First Heading

            Section content.
            """;

        var chunks = KnowledgeChunker.ChunkFromMarkdown(markdown, "guide.md", TestSettings);

        Assert.Equal(2, chunks.Count);
        Assert.Equal("Preamble", chunks[0].Metadata.Section);
        Assert.Contains("Intro text before any heading.", chunks[0].Content, StringComparison.Ordinal);
        Assert.Equal("First Heading", chunks[1].Metadata.Section);
    }

    [Fact]
    public void ChunkFromMarkdown_FallsBackToSingleDocumentChunk_WhenNoHeadingsExist()
    {
        var markdown = """
            Plain markdown text without headings.

            Another paragraph stays with it.
            """;

        var chunks = KnowledgeChunker.ChunkFromMarkdown(markdown, "notes.md", new ChatCompleteSettings
        {
            ChunkParagraphTokens = 20
        });

        var chunk = Assert.Single(chunks);
        Assert.Equal("Document", chunk.Metadata.Section);
        Assert.Contains("Plain markdown text without headings.", chunk.Content, StringComparison.Ordinal);
        Assert.Contains("Another paragraph stays with it.", chunk.Content, StringComparison.Ordinal);
    }

    [Fact]
    public void ChunkFromMarkdown_ReturnsEmpty_ForEmptyInput()
    {
        Assert.Empty(KnowledgeChunker.ChunkFromMarkdown(string.Empty, "empty.md", TestSettings));
        Assert.Empty(KnowledgeChunker.ChunkFromMarkdown("   \n\t   ", "empty.md", TestSettings));
    }

    [Fact]
    public void ChunkFromMarkdown_DoesNotSplitOnH4Headings()
    {
        var markdown = """
            # Top

            Intro paragraph.

            #### Deep Heading

            Deep content stays in the same section.
            """;

        var chunks = KnowledgeChunker.ChunkFromMarkdown(markdown, "deep.md", TestSettings);

        Assert.True(chunks.Count >= 1);
        Assert.All(chunks, chunk => Assert.Equal("Top", chunk.Metadata.Section));
        Assert.Contains(chunks, chunk => chunk.Content.Contains("#### Deep Heading", StringComparison.Ordinal));
    }

    [Fact]
    public void ChunkFromMarkdown_SplitsOversizedParagraphsByWord_AndKeepsHeading()
    {
        var markdown = """
            ## Oversized

            alpha beta gamma delta epsilon zeta eta theta iota kappa lambda mu nu xi omicron pi rho sigma tau upsilon phi chi psi omega
            """;

        var chunks = KnowledgeChunker.ChunkFromMarkdown(markdown, "oversized.md", new ChatCompleteSettings
        {
            ChunkParagraphTokens = 8
        });

        Assert.True(chunks.Count > 1);
        Assert.All(chunks, chunk =>
        {
            Assert.StartsWith("## Oversized", chunk.Content, StringComparison.Ordinal);
            Assert.Equal("Oversized", chunk.Metadata.Section);
        });
    }

    [Fact]
    public void ChunkFromMarkdown_UsesWordCountInsteadOfCharacterHeuristic()
    {
        var markdown = """
            ## Tokens

            supercalifragilisticexpialidocious pneumonoultramicroscopicsilicovolcanoconiosis hippopotomonstrosesquipedaliophobia
            """;

        var chunks = KnowledgeChunker.ChunkFromMarkdown(markdown, "tokens.md", new ChatCompleteSettings
        {
            ChunkParagraphTokens = 5
        });

        var chunk = Assert.Single(chunks);
        Assert.Contains("supercalifragilisticexpialidocious", chunk.Content, StringComparison.Ordinal);
        Assert.Contains("pneumonoultramicroscopicsilicovolcanoconiosis", chunk.Content, StringComparison.Ordinal);
        Assert.Contains("hippopotomonstrosesquipedaliophobia", chunk.Content, StringComparison.Ordinal);
    }

    [Fact]
    public void ChunkFromMarkdown_SplitsSingleWordOverflowAcrossMultipleChunks()
    {
        var markdown = """
            ## SingleWord

            alpha beta gamma delta epsilon zeta

            supercalifragilisticexpialidocious
            """;

        var chunks = KnowledgeChunker.ChunkFromMarkdown(markdown, "single-word.md", new ChatCompleteSettings
        {
            ChunkParagraphTokens = 3
        });

        Assert.True(chunks.Count >= 3);
        Assert.All(chunks, chunk => Assert.StartsWith("## SingleWord", chunk.Content, StringComparison.Ordinal));
        Assert.Contains(chunks, chunk => chunk.Content.Contains("supercalifragilisticexpialidocious", StringComparison.Ordinal));
    }

    [Fact]
    public void ChunkFromMarkdown_PreservesHeadingAcrossParagraphBasedSubChunks()
    {
        var markdown = """
            ## Section

            First paragraph has enough words to stand on its own for the chosen token limit.

            Second paragraph also has enough words to force a second chunk in the same section.
            """;

        var chunks = KnowledgeChunker.ChunkFromMarkdown(markdown, "section.md", new ChatCompleteSettings
        {
            ChunkParagraphTokens = 12
        });

        Assert.True(chunks.Count >= 2);
        Assert.All(chunks, chunk => Assert.StartsWith("## Section", chunk.Content, StringComparison.Ordinal));
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

    [Fact]
    public void ChunkFromPlainText_ReturnsEmpty_ForEmptyInput()
    {
        Assert.Empty(KnowledgeChunker.ChunkFromPlainText(null, "notes.txt", TestSettings));
        Assert.Empty(KnowledgeChunker.ChunkFromPlainText(string.Empty, "notes.txt", TestSettings));
        Assert.Empty(KnowledgeChunker.ChunkFromPlainText("   \n\t   ", "notes.txt", TestSettings));
    }
}
