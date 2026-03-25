using ChatCompletion.Config;
using Xunit;

namespace KnowledgeManager.Tests.KnowledgeEngine;

public class KnowledgeChunkerTests
{
    private const string Source = "test.md";

    private static ChatCompleteSettings MakeSettings(int tokenLimit = 50)
    {
        return new ChatCompleteSettings { ChunkParagraphTokens = tokenLimit };
    }

    // ─── Heading split ───────────────────────────────────────────────

    [Fact]
    public void ChunkFromMarkdown_SplitsOnH1H2H3()
    {
        var md = "# One\nContent1\n## Two\nContent2\n### Three\nContent3";
        var chunks = KnowledgeChunker.ChunkFromMarkdown(md, Source, MakeSettings(500));

        Assert.Equal(3, chunks.Count);
        Assert.Equal("One", chunks[0].Metadata.Section);
        Assert.Equal("Two", chunks[1].Metadata.Section);
        Assert.Equal("Three", chunks[2].Metadata.Section);
    }

    [Fact]
    public void ChunkFromMarkdown_H4AndBeyondAreNotSplitBoundaries()
    {
        var md = "# Title\nSome text\n#### Subheading\nMore text";
        var chunks = KnowledgeChunker.ChunkFromMarkdown(md, Source, MakeSettings(500));

        Assert.Single(chunks);
        Assert.Contains("#### Subheading", chunks[0].Content);
    }

    // ─── Preamble ────────────────────────────────────────────────────

    [Fact]
    public void ChunkFromMarkdown_PreambleBeforeFirstHeading()
    {
        var md = "Some intro text\n\n# First Section\nBody";
        var chunks = KnowledgeChunker.ChunkFromMarkdown(md, Source, MakeSettings(500));

        Assert.Equal(2, chunks.Count);
        Assert.Equal("Preamble", chunks[0].Metadata.Section);
        Assert.Equal("Some intro text", chunks[0].Content);
        Assert.Equal("First Section", chunks[1].Metadata.Section);
    }

    [Fact]
    public void ChunkFromMarkdown_NoPreambleWhenFirstLineIsHeading()
    {
        var md = "# Title\nContent";
        var chunks = KnowledgeChunker.ChunkFromMarkdown(md, Source, MakeSettings(500));

        Assert.Single(chunks);
        Assert.Equal("Title", chunks[0].Metadata.Section);
    }

    // ─── Plain text fallback ─────────────────────────────────────────

    [Fact]
    public void ChunkFromMarkdown_NoHeadings_TreatsAsDocument()
    {
        var md = "Just some plain text\nwith multiple lines";
        var chunks = KnowledgeChunker.ChunkFromMarkdown(md, Source, MakeSettings(500));

        Assert.Single(chunks);
        Assert.Equal("Document", chunks[0].Metadata.Section);
    }

    [Fact]
    public void ChunkFromPlainText_SplitsByParagraphs()
    {
        var text = "Para one words.\n\nPara two words.\n\nPara three words.";
        var chunks = KnowledgeChunker.ChunkFromPlainText(text, Source, MakeSettings(5));

        Assert.True(chunks.Count >= 2);
        Assert.All(chunks, c => Assert.Equal("Document", c.Metadata.Section));
    }

    // ─── Empty / null input ──────────────────────────────────────────

    [Fact]
    public void ChunkFromMarkdown_EmptyInput_ReturnsEmpty()
    {
        Assert.Empty(KnowledgeChunker.ChunkFromMarkdown("", Source));
        Assert.Empty(KnowledgeChunker.ChunkFromMarkdown(null!, Source));
        Assert.Empty(KnowledgeChunker.ChunkFromMarkdown("   ", Source));
    }

    [Fact]
    public void ChunkFromPlainText_EmptyInput_ReturnsEmpty()
    {
        Assert.Empty(KnowledgeChunker.ChunkFromPlainText("", Source));
        Assert.Empty(KnowledgeChunker.ChunkFromPlainText(null, Source));
        Assert.Empty(KnowledgeChunker.ChunkFromPlainText("   ", Source));
    }

    // ─── Sub-chunking ────────────────────────────────────────────────

    [Fact]
    public void ChunkFromMarkdown_OversizedSection_SubChunksByParagraph()
    {
        // 5-word limit means "# Title" (2 tokens) + body must fit in 5 tokens total
        var md = "# Title\nPara one words here.\n\nPara two words here.";
        var chunks = KnowledgeChunker.ChunkFromMarkdown(md, Source, MakeSettings(5));

        Assert.True(chunks.Count >= 2, $"Expected sub-chunking, got {chunks.Count} chunk(s)");
        Assert.All(chunks, c => Assert.Equal("Title", c.Metadata.Section));
    }

    [Fact]
    public void ChunkFromMarkdown_SubChunks_PreserveHeadingInEachChunk()
    {
        var md = "## Setup\nFirst paragraph words.\n\nSecond paragraph words.";
        var chunks = KnowledgeChunker.ChunkFromMarkdown(md, Source, MakeSettings(5));

        Assert.True(chunks.Count >= 2);
        Assert.All(chunks, c =>
        {
            Assert.StartsWith("## Setup", c.Content);
        });
    }

    [Fact]
    public void ChunkFromMarkdown_OversizedParagraph_SplitsByWord()
    {
        // Single paragraph with many words, tiny token limit
        var md = "# Section\n" + string.Join(" ", Enumerable.Range(1, 30).Select(i => $"word{i}"));
        var chunks = KnowledgeChunker.ChunkFromMarkdown(md, Source, MakeSettings(8));

        Assert.True(chunks.Count >= 3, $"Expected word-level splitting, got {chunks.Count}");
        Assert.All(chunks, c => Assert.StartsWith("# Section", c.Content));
    }

    // ─── Token estimation ────────────────────────────────────────────

    [Fact]
    public void CountTokens_UsesWordCount()
    {
        Assert.Equal(4, KnowledgeChunker.CountTokens("hello world foo bar"));
        Assert.Equal(1, KnowledgeChunker.CountTokens("hello"));
        Assert.Equal(0, KnowledgeChunker.CountTokens(""));
        Assert.Equal(0, KnowledgeChunker.CountTokens(null));
        Assert.Equal(0, KnowledgeChunker.CountTokens("   "));
    }

    [Fact]
    public void CountTokens_HandlesSpecialCharacters()
    {
        // Punctuation attached to words counts as one token
        Assert.Equal(3, KnowledgeChunker.CountTokens("hello, world! foo"));
        // Markdown heading syntax
        Assert.Equal(3, KnowledgeChunker.CountTokens("## My Title"));
    }

    // ─── Metadata ────────────────────────────────────────────────────

    [Fact]
    public void ChunkFromMarkdown_SetsSourceMetadata()
    {
        var md = "# Title\nContent";
        var chunks = KnowledgeChunker.ChunkFromMarkdown(md, "my-doc.md", MakeSettings(500));

        Assert.All(chunks, c => Assert.Equal("my-doc.md", c.Metadata.Source));
    }

    [Fact]
    public void ChunkFromMarkdown_GeneratesTags()
    {
        var md = "# Docker Deployment\nContent here";
        var chunks = KnowledgeChunker.ChunkFromMarkdown(md, Source, MakeSettings(500));

        var tags = chunks[0].Metadata.Tags;
        Assert.Contains("docker", tags);
        Assert.Contains("deployment", tags);
    }

    [Fact]
    public void GenerateTags_DeduplicatesAndStrips()
    {
        var tags = KnowledgeChunker.GenerateTags("API: API Endpoints");
        Assert.Contains("api", tags);
        Assert.Contains("endpoints", tags);
        // "api" should only appear once (deduplication)
        Assert.Equal(tags.Distinct().Count(), tags.Length);
    }

    // ─── ParseMarkdownSections ───────────────────────────────────────

    [Fact]
    public void ParseMarkdownSections_SeparatesHeadingFromBody()
    {
        var md = "# My Title\nBody text here\n\nMore body";
        var sections = KnowledgeChunker.ParseMarkdownSections(md).ToList();

        Assert.Single(sections);
        Assert.Equal("# My Title", sections[0].HeadingLine);
        Assert.Equal("My Title", sections[0].Title);
        Assert.Equal("Body text here\n\nMore body", sections[0].Body);
    }

    [Fact]
    public void ParseMarkdownSections_NoHeadings_ReturnsSingleDocument()
    {
        var md = "Just text";
        var sections = KnowledgeChunker.ParseMarkdownSections(md).ToList();

        Assert.Single(sections);
        Assert.Null(sections[0].HeadingLine);
        Assert.Equal("Document", sections[0].Title);
    }

    [Fact]
    public void ParseMarkdownSections_MultipleSections()
    {
        var md = "# One\nA\n## Two\nB\n### Three\nC";
        var sections = KnowledgeChunker.ParseMarkdownSections(md).ToList();

        Assert.Equal(3, sections.Count);
        Assert.Equal("One", sections[0].Title);
        Assert.Equal("Two", sections[1].Title);
        Assert.Equal("Three", sections[2].Title);
    }

    // ─── ComposeChunkContent ─────────────────────────────────────────

    [Fact]
    public void ComposeChunkContent_WithHeading_PrependsHeading()
    {
        var result = KnowledgeChunker.ComposeChunkContent("## Title", new[] { "Body text" });
        Assert.Equal("## Title\n\nBody text", result);
    }

    [Fact]
    public void ComposeChunkContent_WithoutHeading_BodyOnly()
    {
        var result = KnowledgeChunker.ComposeChunkContent(null, new[] { "Body text" });
        Assert.Equal("Body text", result);
    }

    [Fact]
    public void ComposeChunkContent_MultipleParagraphs_JoinedByDoubleNewline()
    {
        var result = KnowledgeChunker.ComposeChunkContent("# H", new[] { "Para1", "Para2" });
        Assert.Equal("# H\n\nPara1\n\nPara2", result);
    }

    // ─── Config resolution ───────────────────────────────────────────

    [Fact]
    public void ChunkFromMarkdown_RespectsChunkParagraphTokens()
    {
        var settings = MakeSettings(3);
        var md = "# Title\nWord1 word2 word3 word4 word5 word6";
        var chunks = KnowledgeChunker.ChunkFromMarkdown(md, Source, settings);

        // With 3-token limit, should produce multiple chunks
        Assert.True(chunks.Count > 1);
    }

    // ─── Edge cases ──────────────────────────────────────────────────

    [Fact]
    public void ChunkFromMarkdown_HeadingOnly_NoBody()
    {
        var md = "# Just A Heading";
        var chunks = KnowledgeChunker.ChunkFromMarkdown(md, Source, MakeSettings(500));

        Assert.Single(chunks);
        Assert.Equal("# Just A Heading", chunks[0].Content);
    }

    [Fact]
    public void ChunkFromMarkdown_CrlfNormalized()
    {
        var md = "# Title\r\nContent\r\n\r\n## Next\r\nMore";
        var chunks = KnowledgeChunker.ChunkFromMarkdown(md, Source, MakeSettings(500));

        Assert.Equal(2, chunks.Count);
    }

    [Fact]
    public void ChunkFromMarkdown_WhitespaceOnlySection_Skipped()
    {
        var md = "# Title\n\n\n\n## Next\nContent";
        var chunks = KnowledgeChunker.ChunkFromMarkdown(md, Source, MakeSettings(500));

        // Empty section after "# Title" should still produce the heading chunk
        Assert.True(chunks.Count >= 1);
        Assert.Contains(chunks, c => c.Metadata.Section == "Next");
    }

    [Fact]
    public void ChunkFromMarkdown_PreservesCodeBlocks()
    {
        var md = "# Code\n```csharp\nvar x = 1;\n```";
        var chunks = KnowledgeChunker.ChunkFromMarkdown(md, Source, MakeSettings(500));

        Assert.Single(chunks);
        Assert.Contains("```csharp", chunks[0].Content);
        Assert.Contains("var x = 1;", chunks[0].Content);
    }

    [Fact]
    public void ChunkFromMarkdown_RealWorldDocument()
    {
        var md = @"# Project Overview
This is a knowledge management system.
It supports multiple LLM providers.

## Architecture
The backend uses ASP.NET 8 with minimal APIs.
Serilog handles logging. SQLite stores configuration.

### Vector Store
Qdrant is the primary vector database.
MongoDB Atlas is supported as an alternative.

## Deployment
Docker containers are used for deployment.
The image is published to Docker Hub.";

        var chunks = KnowledgeChunker.ChunkFromMarkdown(md, Source, MakeSettings(500));

        Assert.Equal(4, chunks.Count);
        Assert.Equal("Project Overview", chunks[0].Metadata.Section);
        Assert.Equal("Architecture", chunks[1].Metadata.Section);
        Assert.Equal("Vector Store", chunks[2].Metadata.Section);
        Assert.Equal("Deployment", chunks[3].Metadata.Section);
    }
}
