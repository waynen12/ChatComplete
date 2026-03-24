using ChatCompletion.Config;
using Xunit;
using Xunit.Abstractions;

namespace KnowledgeManager.Tests.KnowledgeEngine;

/// <summary>
/// Comprehensive tests for the synthesised markdown-aware KnowledgeChunker.
/// Covers heading splitting, preamble, sub-chunking, oversized paragraphs,
/// heading preservation, empty input, plain-text fallback, and token counting.
/// </summary>
public class KnowledgeChunkerTests
{
    private readonly ITestOutputHelper _output;

    public KnowledgeChunkerTests(ITestOutputHelper output)
    {
        _output = output;
        SettingsProvider.Initialize(new ChatCompleteSettings
        {
            ChunkParagraphTokens = 200,
            ChunkCharacterLimit = 4096,
            ChunkOverlap = 40
        });
    }

    // ── ChunkFromMarkdown — heading split ─────────────────────────────────────────

    [Fact]
    public void ChunkFromMarkdown_SplitsOnH1H2H3_OneChunkPerSection()
    {
        var markdown = "# Heading 1\n\nContent 1.\n\n## Heading 2\n\nContent 2.\n\n### Heading 3\n\nContent 3.";
        var chunks = KnowledgeChunker.ChunkFromMarkdown(markdown, "test.md", maxTokens: 200);

        Assert.Equal(3, chunks.Count);
        Assert.Equal("Heading 1", chunks[0].Metadata.Section);
        Assert.Equal("Heading 2", chunks[1].Metadata.Section);
        Assert.Equal("Heading 3", chunks[2].Metadata.Section);
    }

    [Fact]
    public void ChunkFromMarkdown_H4AndBeyondAreNotSplitPoints()
    {
        var markdown = "# Main\n\nIntro.\n\n#### Deep Heading\n\nDeep content.";
        var chunks = KnowledgeChunker.ChunkFromMarkdown(markdown, "test.md", maxTokens: 500);

        // H4 should NOT split — the deep heading stays with the parent section
        Assert.Single(chunks);
        Assert.Contains("#### Deep Heading", chunks[0].Content);
    }

    [Fact]
    public void ChunkFromMarkdown_EachChunkContainsItsHeading()
    {
        var markdown = "# One\n\nContent one.\n\n## Two\n\nContent two.";
        var chunks = KnowledgeChunker.ChunkFromMarkdown(markdown, "test.md", maxTokens: 500);

        Assert.Equal(2, chunks.Count);
        Assert.StartsWith("# One", chunks[0].Content);
        Assert.StartsWith("## Two", chunks[1].Content);
    }

    [Fact]
    public void ChunkFromMarkdown_NoHeadings_ReturnsSingleDocumentChunk()
    {
        var markdown = "Just some plain text.\n\nAnother paragraph.";
        var chunks = KnowledgeChunker.ChunkFromMarkdown(markdown, "test.md", maxTokens: 500);

        Assert.Single(chunks);
        Assert.Equal("Document", chunks[0].Metadata.Section);
    }

    // ── ChunkFromMarkdown — preamble ──────────────────────────────────────────────

    [Fact]
    public void ChunkFromMarkdown_PreambleBeforeFirstHeading_IsOwnChunk()
    {
        var markdown = "Some preamble text.\n\n# First Section\n\nBody.";
        var chunks = KnowledgeChunker.ChunkFromMarkdown(markdown, "test.md", maxTokens: 500);

        Assert.Equal(2, chunks.Count);
        Assert.Equal("Introduction", chunks[0].Metadata.Section);
        Assert.Contains("preamble", chunks[0].Content);
        Assert.Equal("First Section", chunks[1].Metadata.Section);
    }

    [Fact]
    public void ChunkFromMarkdown_NoPreamble_NoIntroductionChunk()
    {
        var markdown = "# Only Section\n\nBody.";
        var chunks = KnowledgeChunker.ChunkFromMarkdown(markdown, "test.md", maxTokens: 500);

        Assert.Single(chunks);
        Assert.Equal("Only Section", chunks[0].Metadata.Section);
    }

    // ── ChunkFromMarkdown — sub-chunking ──────────────────────────────────────────

    [Fact]
    public void ChunkFromMarkdown_OversizedSection_IsSubChunkedByParagraph()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("# Large Section");
        sb.AppendLine();
        for (int i = 0; i < 10; i++)
        {
            sb.AppendLine($"Paragraph {i}: {new string('x', 50)}");
            sb.AppendLine();
        }

        // With a small limit the 10 paragraphs must span multiple chunks
        var chunks = KnowledgeChunker.ChunkFromMarkdown(sb.ToString(), "big.md", maxTokens: 20);

        Assert.True(chunks.Count > 1, $"Expected sub-chunks, got {chunks.Count}");
        Assert.All(chunks, c => Assert.Equal("Large Section", c.Metadata.Section));

        _output.WriteLine($"Large section → {chunks.Count} sub-chunks");
    }

    [Fact]
    public void ChunkFromMarkdown_SmallSection_NotSubChunked()
    {
        var markdown = "# Small\n\nFew words.";
        var chunks = KnowledgeChunker.ChunkFromMarkdown(markdown, "test.md", maxTokens: 500);

        Assert.Single(chunks);
        Assert.Equal("Small", chunks[0].Metadata.Section);
    }

    // ── ChunkFromMarkdown — heading preserved in sub-chunks ───────────────────────

    [Fact]
    public void ChunkFromMarkdown_SubChunks_EachStartsWithHeading()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("## My Section");
        sb.AppendLine();
        for (int i = 0; i < 20; i++)
        {
            sb.AppendLine($"Line {i}: {new string('a', 60)}");
            sb.AppendLine();
        }

        var chunks = KnowledgeChunker.ChunkFromMarkdown(sb.ToString(), "test.md", maxTokens: 20);

        Assert.True(chunks.Count > 1);
        foreach (var chunk in chunks)
            Assert.StartsWith("## My Section", chunk.Content);
    }

    // ── ChunkFromMarkdown — oversized single paragraph ────────────────────────────

    [Fact]
    public void ChunkFromMarkdown_OversizedSingleParagraph_SplitAtWordBoundaries()
    {
        // 25 distinct words — with a limit of 8 tokens, the paragraph must produce multiple chunks
        var words = string.Join(" ", Enumerable.Range(1, 25).Select(i => $"word{i}"));
        var markdown = $"## Oversized\n\n{words}";

        var chunks = KnowledgeChunker.ChunkFromMarkdown(markdown, "oversized.md", maxTokens: 10);

        Assert.True(chunks.Count >= 2, $"Expected word-level sub-chunks, got {chunks.Count}");
        Assert.All(chunks, c => Assert.Equal("Oversized", c.Metadata.Section));
        Assert.All(chunks, c => Assert.StartsWith("## Oversized", c.Content));
    }

    // ── ChunkFromMarkdown — empty / whitespace ────────────────────────────────────

    [Fact]
    public void ChunkFromMarkdown_EmptyString_ReturnsEmpty()
    {
        Assert.Empty(KnowledgeChunker.ChunkFromMarkdown("", "test.md", maxTokens: 200));
    }

    [Fact]
    public void ChunkFromMarkdown_WhitespaceOnly_ReturnsEmpty()
    {
        Assert.Empty(KnowledgeChunker.ChunkFromMarkdown("   \n\n   ", "test.md", maxTokens: 200));
    }

    // ── ChunkFromMarkdown — metadata ──────────────────────────────────────────────

    [Fact]
    public void ChunkFromMarkdown_PreservesSourceMetadata()
    {
        var markdown = "# Test\n\nContent.";
        var chunks = KnowledgeChunker.ChunkFromMarkdown(markdown, "document.md", maxTokens: 500);

        Assert.Single(chunks);
        Assert.Equal("document.md", chunks[0].Metadata.Source);
    }

    [Fact]
    public void ChunkFromMarkdown_GeneratesTagsFromSectionTitle()
    {
        var markdown = "# Docker Deployment Guide\n\nContent.";
        var chunks = KnowledgeChunker.ChunkFromMarkdown(markdown, "test.md", maxTokens: 500);

        Assert.Contains("docker", chunks[0].Metadata.Tags);
        Assert.Contains("deployment", chunks[0].Metadata.Tags);
        Assert.Contains("guide", chunks[0].Metadata.Tags);
    }

    // ── ChunkFromMarkdown — settings fallback ─────────────────────────────────────

    [Fact]
    public void ChunkFromMarkdown_ZeroMaxTokens_UsesSettingsChunkParagraphTokens()
    {
        SettingsProvider.Initialize(new ChatCompleteSettings { ChunkParagraphTokens = 10 });
        try
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("# Section");
            sb.AppendLine();
            for (int i = 0; i < 5; i++)
            {
                sb.AppendLine($"Paragraph {i} with enough words to exceed ten tokens per chunk when combined together.");
                sb.AppendLine();
            }

            // maxTokens=0 → reads from settings (10 tokens)
            var chunks = KnowledgeChunker.ChunkFromMarkdown(sb.ToString(), "test.md");

            Assert.True(chunks.Count > 1, "Should sub-chunk when settings limit is small");
        }
        finally
        {
            SettingsProvider.Initialize(new ChatCompleteSettings { ChunkParagraphTokens = 200 });
        }
    }

    // ── ChunkFromMarkdown — multiple H2 under H1 ─────────────────────────────────

    [Fact]
    public void ChunkFromMarkdown_MultipleSubsections_SeparateChunks()
    {
        var markdown = "# Main\n\nOverview.\n\n## Sub A\n\nDetails A.\n\n## Sub B\n\nDetails B.";
        var chunks = KnowledgeChunker.ChunkFromMarkdown(markdown, "test.md", maxTokens: 500);

        Assert.Equal(3, chunks.Count);
        Assert.Equal("Main", chunks[0].Metadata.Section);
        Assert.Equal("Sub A", chunks[1].Metadata.Section);
        Assert.Equal("Sub B", chunks[2].Metadata.Section);
    }

    // ── ChunkFromPlainText ────────────────────────────────────────────────────────

    [Fact]
    public void ChunkFromPlainText_ShortText_SingleChunk()
    {
        var text = "Some short plain text.";
        var chunks = KnowledgeChunker.ChunkFromPlainText(text, "notes.txt");

        Assert.Single(chunks);
        Assert.Equal("notes.txt", chunks[0].Metadata.Source);
        Assert.Equal("Document", chunks[0].Metadata.Section);
    }

    [Fact]
    public void ChunkFromPlainText_NullInput_ReturnsEmpty()
    {
        Assert.Empty(KnowledgeChunker.ChunkFromPlainText(null, "test.txt"));
    }

    [Fact]
    public void ChunkFromPlainText_EmptyString_ReturnsEmpty()
    {
        Assert.Empty(KnowledgeChunker.ChunkFromPlainText("", "test.txt"));
    }

    [Fact]
    public void ChunkFromPlainText_LargeText_ProducesMultipleChunks()
    {
        SettingsProvider.Initialize(new ChatCompleteSettings { ChunkParagraphTokens = 15 });
        try
        {
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < 10; i++)
            {
                sb.AppendLine($"Paragraph {i}: alpha beta gamma delta epsilon zeta eta theta iota kappa lambda.");
                sb.AppendLine();
            }

            var chunks = KnowledgeChunker.ChunkFromPlainText(sb.ToString(), "large.txt");

            Assert.True(chunks.Count > 1, $"Expected multiple chunks, got {chunks.Count}");
            Assert.All(chunks, c => Assert.Equal("Document", c.Metadata.Section));
        }
        finally
        {
            SettingsProvider.Initialize(new ChatCompleteSettings { ChunkParagraphTokens = 200 });
        }
    }

    // ── CountTokens ───────────────────────────────────────────────────────────────

    [Fact]
    public void CountTokens_EmptyString_ReturnsZero()
    {
        Assert.Equal(0, KnowledgeChunker.CountTokens(""));
    }

    [Fact]
    public void CountTokens_NullString_ReturnsZero()
    {
        Assert.Equal(0, KnowledgeChunker.CountTokens(null));
    }

    [Fact]
    public void CountTokens_CountsWhitespaceSeparatedWords()
    {
        // Each \S+ sequence is one token
        Assert.Equal(1, KnowledgeChunker.CountTokens("hello"));
        Assert.Equal(3, KnowledgeChunker.CountTokens("one two three"));
        Assert.Equal(2, KnowledgeChunker.CountTokens("  alpha   beta  "));
    }

    [Fact]
    public void CountTokens_PunctuationAttachedToWord_CountsAsOneToken()
    {
        // "hello," is one \S+ token
        Assert.Equal(1, KnowledgeChunker.CountTokens("hello,"));
        Assert.Equal(2, KnowledgeChunker.CountTokens("hello, world."));
    }

    // ── Integration-style tests ───────────────────────────────────────────────────

    [Fact]
    public void ChunkFromMarkdown_RealWorldDocument_ProducesReasonableChunkCount()
    {
        var markdown = @"# Project Overview

This is a full-stack application for managing knowledge bases.

## Architecture

The system uses a microservices architecture with the following components:

- **Backend**: ASP.NET 8 with Minimal APIs
- **Frontend**: React with TypeScript
- **Database**: SQLite for configuration, Qdrant for vectors
- **AI**: Multiple LLM providers (OpenAI, Gemini, Ollama, Anthropic)

## Getting Started

### Prerequisites

- .NET 8 SDK
- Node.js 18+
- Docker (optional)

### Installation

1. Clone the repository
2. Run `dotnet build`
3. Start the development server

## Configuration

Settings are stored in `appsettings.json`.

## Deployment

Deploy using Docker Compose for the easiest setup.
";

        var chunks = KnowledgeChunker.ChunkFromMarkdown(markdown, "README.md", maxTokens: 500);

        _output.WriteLine($"Real-world doc → {chunks.Count} chunks");
        foreach (var chunk in chunks)
            _output.WriteLine($"  [{chunk.Metadata.Section}] ({KnowledgeChunker.CountTokens(chunk.Content)} tokens)");

        Assert.True(chunks.Count >= 4, $"Expected at least 4 sections, got {chunks.Count}");
        Assert.All(chunks, c =>
        {
            Assert.False(string.IsNullOrWhiteSpace(c.Content));
            Assert.False(string.IsNullOrWhiteSpace(c.Metadata.Source));
            Assert.False(string.IsNullOrWhiteSpace(c.Metadata.Section));
            Assert.NotEmpty(c.Metadata.Tags);
        });
    }

    [Fact]
    public void ChunkFromMarkdown_LargeDocumentDoesNotExplodeIntoTinyChunks()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("# Documentation");
        sb.AppendLine();
        for (int s = 0; s < 5; s++)
        {
            sb.AppendLine($"## Section {s}");
            sb.AppendLine();
            for (int p = 0; p < 10; p++)
            {
                sb.AppendLine($"Content paragraph {p} in section {s}. Normal paragraph of readable text.");
                sb.AppendLine();
            }
        }

        var chunks = KnowledgeChunker.ChunkFromMarkdown(sb.ToString(), "large-doc.md", maxTokens: 200);

        _output.WriteLine($"Large doc: {sb.Length} chars → {chunks.Count} chunks");

        Assert.True(chunks.Count >= 5, $"Too few chunks ({chunks.Count})");
        Assert.True(chunks.Count <= 60, $"Too many chunks ({chunks.Count}) — over-fragmented");
    }
}
