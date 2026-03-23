using Xunit;
using ChatCompletion.Config;

public class KnowledgeChunkerTests
{
    private const string Source = "test.md";

    [Fact]
    public void ChunkFromMarkdown_SplitsOnH1H2H3()
    {
        var md = """
            # Heading 1
            Content under h1.

            ## Heading 2
            Content under h2.

            ### Heading 3
            Content under h3.
            """;

        var chunks = KnowledgeChunker.ChunkFromMarkdown(md, Source, maxTokens: 5000);

        Assert.Equal(3, chunks.Count);
        Assert.Equal("Heading 1", chunks[0].Metadata.Section);
        Assert.Equal("Heading 2", chunks[1].Metadata.Section);
        Assert.Equal("Heading 3", chunks[2].Metadata.Section);
    }

    [Fact]
    public void ChunkFromMarkdown_PreservesContentBeforeFirstHeading()
    {
        var md = """
            Some preamble text here.

            # First Section
            Body of section.
            """;

        var chunks = KnowledgeChunker.ChunkFromMarkdown(md, Source, maxTokens: 5000);

        Assert.Equal(2, chunks.Count);
        Assert.Equal("Introduction", chunks[0].Metadata.Section);
        Assert.Contains("preamble", chunks[0].Content);
        Assert.Equal("First Section", chunks[1].Metadata.Section);
    }

    [Fact]
    public void ChunkFromMarkdown_SubChunksOversizedSections()
    {
        // Create a section that exceeds a small token limit
        var longBody = string.Join("\n\n", Enumerable.Range(1, 20).Select(i => $"Paragraph {i} with some filler text to take up tokens."));
        var md = $"# Big Section\n\n{longBody}";

        // Token limit of 50 should force sub-chunking (~4 chars/token, so 200 chars)
        var chunks = KnowledgeChunker.ChunkFromMarkdown(md, Source, maxTokens: 50);

        Assert.True(chunks.Count > 1, $"Expected multiple sub-chunks but got {chunks.Count}");

        // All sub-chunks should reference the section title
        foreach (var chunk in chunks)
        {
            Assert.Contains("Big Section", chunk.Metadata.Section);
            Assert.Equal(Source, chunk.Metadata.Source);
        }

        // Sub-chunks should have part numbers in metadata
        Assert.Contains("part 1", chunks[0].Metadata.Section);
    }

    [Fact]
    public void ChunkFromMarkdown_SubChunksPreserveHeadingInContent()
    {
        var longBody = string.Join("\n\n", Enumerable.Range(1, 20).Select(i => $"Paragraph {i} with text."));
        var md = $"## My Section\n\n{longBody}";

        var chunks = KnowledgeChunker.ChunkFromMarkdown(md, Source, maxTokens: 50);

        Assert.True(chunks.Count > 1);
        // Each sub-chunk should have the heading prepended for context
        foreach (var chunk in chunks)
        {
            Assert.StartsWith("## My Section", chunk.Content);
        }
    }

    [Fact]
    public void ChunkFromMarkdown_EmptyInput_ReturnsEmpty()
    {
        Assert.Empty(KnowledgeChunker.ChunkFromMarkdown("", Source, maxTokens: 500));
        Assert.Empty(KnowledgeChunker.ChunkFromMarkdown("   ", Source, maxTokens: 500));
    }

    [Fact]
    public void ChunkFromMarkdown_NoHeadings_ReturnsSingleChunk()
    {
        var md = "Just plain text\nwith no headings at all.";

        var chunks = KnowledgeChunker.ChunkFromMarkdown(md, Source, maxTokens: 5000);

        Assert.Single(chunks);
        Assert.Equal("Document", chunks[0].Metadata.Section);
    }

    [Fact]
    public void ChunkFromMarkdown_PreservesSourceMetadata()
    {
        var md = "# Section\nBody.";
        var chunks = KnowledgeChunker.ChunkFromMarkdown(md, "report.pdf", maxTokens: 5000);

        Assert.All(chunks, c => Assert.Equal("report.pdf", c.Metadata.Source));
    }

    [Fact]
    public void ChunkFromMarkdown_GeneratesTagsFromTitle()
    {
        var md = "# Docker Best Practices\nContent.";
        var chunks = KnowledgeChunker.ChunkFromMarkdown(md, Source, maxTokens: 5000);

        Assert.Contains("docker", chunks[0].Metadata.Tags);
        Assert.Contains("best", chunks[0].Metadata.Tags);
        Assert.Contains("practices", chunks[0].Metadata.Tags);
    }

    [Fact]
    public void ChunkFromMarkdown_SmallSectionNotSubChunked()
    {
        var md = "# Small\nJust a tiny section.";

        var chunks = KnowledgeChunker.ChunkFromMarkdown(md, Source, maxTokens: 5000);

        Assert.Single(chunks);
        Assert.Equal("Small", chunks[0].Metadata.Section);
        Assert.DoesNotContain("part", chunks[0].Metadata.Section);
    }

    [Fact]
    public void SplitByHeadings_CorrectlyIdentifiesLevels()
    {
        var md = "# H1\nA\n## H2\nB\n### H3\nC";
        var sections = KnowledgeChunker.SplitByHeadings(md);

        Assert.Equal(3, sections.Count);
        Assert.Equal(1, sections[0].HeadingLevel);
        Assert.Equal(2, sections[1].HeadingLevel);
        Assert.Equal(3, sections[2].HeadingLevel);
    }

    [Fact]
    public void EstimateTokens_ReturnsReasonableEstimate()
    {
        // 100 chars → ~25 tokens
        var text = new string('a', 100);
        int tokens = KnowledgeChunker.EstimateTokens(text);

        Assert.Equal(25, tokens);
    }

    [Fact]
    public void EstimateTokens_EmptyString_ReturnsZero()
    {
        Assert.Equal(0, KnowledgeChunker.EstimateTokens(""));
        Assert.Equal(0, KnowledgeChunker.EstimateTokens(null!));
    }

    [Fact]
    public void ChunkFromPlainText_EmptyInput_ReturnsEmpty()
    {
        SettingsProvider.Initialize(new ChatCompleteSettings { ChunkParagraphTokens = 200 });

        Assert.Empty(KnowledgeChunker.ChunkFromPlainText("", Source));
        Assert.Empty(KnowledgeChunker.ChunkFromPlainText(null, Source));
    }

    [Fact]
    public void ChunkFromPlainText_SplitsByParagraphsRespectingTokenLimit()
    {
        SettingsProvider.Initialize(new ChatCompleteSettings { ChunkParagraphTokens = 30 });

        var text = string.Join("\n\n", Enumerable.Range(1, 10).Select(i => $"Paragraph {i} with enough words to use up some tokens in the estimate."));

        var chunks = KnowledgeChunker.ChunkFromPlainText(text, Source);

        Assert.True(chunks.Count > 1, "Should split into multiple chunks");
        Assert.All(chunks, c => Assert.Equal(Source, c.Metadata.Source));
    }

    [Fact]
    public void ChunkFromMarkdown_H4AndBeyondNotSplitPoints()
    {
        var md = """
            # Top
            Intro.

            #### Deep heading
            This stays with the parent section.
            """;

        var chunks = KnowledgeChunker.ChunkFromMarkdown(md, Source, maxTokens: 5000);

        // h4 is NOT a split point, so content stays with h1
        Assert.Single(chunks);
        Assert.Contains("#### Deep heading", chunks[0].Content);
    }

    [Fact]
    public void ChunkFromMarkdown_MultipleH2UnderH1()
    {
        var md = """
            # Main Topic
            Overview.

            ## Sub A
            Details about A.

            ## Sub B
            Details about B.

            ## Sub C
            Details about C.
            """;

        var chunks = KnowledgeChunker.ChunkFromMarkdown(md, Source, maxTokens: 5000);

        Assert.Equal(4, chunks.Count);
        Assert.Equal("Main Topic", chunks[0].Metadata.Section);
        Assert.Equal("Sub A", chunks[1].Metadata.Section);
        Assert.Equal("Sub B", chunks[2].Metadata.Section);
        Assert.Equal("Sub C", chunks[3].Metadata.Section);
    }
}
