using ChatCompletion.Config;
using Xunit;
using Xunit.Abstractions;

namespace KnowledgeManager.Tests.KnowledgeEngine
{
    public class KnowledgeChunkerTests
    {
        private readonly ITestOutputHelper _output;

        public KnowledgeChunkerTests(ITestOutputHelper output)
        {
            _output = output;
            // Initialize settings for tests
            SettingsProvider.Initialize(new ChatCompleteSettings
            {
                ChunkParagraphTokens = 200,
                ChunkCharacterLimit = 4096,
                ChunkOverlap = 40
            });
        }

        // ─── SplitOnHeadings Tests ───

        [Fact]
        public void SplitOnHeadings_SingleH1_ReturnsOneSection()
        {
            var markdown = "# Title\n\nSome content here.";
            var sections = KnowledgeChunker.SplitOnHeadings(markdown);

            Assert.Single(sections);
            Assert.Equal("Title", sections[0].Title);
            Assert.Equal(1, sections[0].Level);
            Assert.Contains("Some content here.", sections[0].Content);
        }

        [Fact]
        public void SplitOnHeadings_MultipleHeadings_ReturnsSectionsPerHeading()
        {
            var markdown = "# Heading 1\n\nContent 1.\n\n## Heading 2\n\nContent 2.\n\n### Heading 3\n\nContent 3.";
            var sections = KnowledgeChunker.SplitOnHeadings(markdown);

            Assert.Equal(3, sections.Count);
            Assert.Equal("Heading 1", sections[0].Title);
            Assert.Equal(1, sections[0].Level);
            Assert.Equal("Heading 2", sections[1].Title);
            Assert.Equal(2, sections[1].Level);
            Assert.Equal("Heading 3", sections[2].Title);
            Assert.Equal(3, sections[2].Level);
        }

        [Fact]
        public void SplitOnHeadings_NoHeadings_ReturnsSingleDocumentSection()
        {
            var markdown = "Just some plain text without any headings.\n\nAnother paragraph.";
            var sections = KnowledgeChunker.SplitOnHeadings(markdown);

            Assert.Single(sections);
            Assert.Equal("Document", sections[0].Title);
            Assert.Equal(0, sections[0].Level);
        }

        [Fact]
        public void SplitOnHeadings_PreambleBeforeFirstHeading_IncludesPreamble()
        {
            var markdown = "Some preamble text.\n\n# First Heading\n\nContent.";
            var sections = KnowledgeChunker.SplitOnHeadings(markdown);

            Assert.Equal(2, sections.Count);
            Assert.Equal("Preamble", sections[0].Title);
            Assert.Contains("preamble text", sections[0].Content);
            Assert.Equal("First Heading", sections[1].Title);
        }

        [Fact]
        public void SplitOnHeadings_H4AndBeyond_NotSplitOn()
        {
            var markdown = "# Main\n\nContent.\n\n#### Deep Heading\n\nDeep content.";
            var sections = KnowledgeChunker.SplitOnHeadings(markdown);

            // H4 should NOT cause a split — only h1/h2/h3
            Assert.Single(sections);
            Assert.Contains("#### Deep Heading", sections[0].Content);
        }

        [Fact]
        public void SplitOnHeadings_EachSectionContainsItsHeading()
        {
            var markdown = "# One\n\nContent one.\n\n## Two\n\nContent two.";
            var sections = KnowledgeChunker.SplitOnHeadings(markdown);

            Assert.Contains("# One", sections[0].Content);
            Assert.Contains("## Two", sections[1].Content);
        }

        // ─── ChunkFromMarkdown Tests ───

        [Fact]
        public void ChunkFromMarkdown_SmallSections_OneChunkPerSection()
        {
            var markdown = "# Section A\n\nShort content.\n\n## Section B\n\nAlso short.";
            var chunks = KnowledgeChunker.ChunkFromMarkdown(markdown, "test.md", tokenLimit: 200);

            Assert.Equal(2, chunks.Count);
            Assert.Equal("test.md", chunks[0].Metadata.Source);
            Assert.Equal("Section A", chunks[0].Metadata.Section);
            Assert.Equal("Section B", chunks[1].Metadata.Section);
        }

        [Fact]
        public void ChunkFromMarkdown_LargeSection_SubChunked()
        {
            // Create a section with many paragraphs exceeding the token limit
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("# Large Section");
            sb.AppendLine();
            for (int i = 0; i < 20; i++)
            {
                sb.AppendLine($"Paragraph {i}: {new string('x', 200)}");
                sb.AppendLine();
            }

            var chunks = KnowledgeChunker.ChunkFromMarkdown(sb.ToString(), "big.md", tokenLimit: 100);

            Assert.True(chunks.Count > 1, $"Expected multiple sub-chunks, got {chunks.Count}");
            // All chunks should reference the same section
            Assert.All(chunks, c => Assert.Contains("Large Section", c.Metadata.Section));
            // First chunk should not have "(part N)" suffix
            Assert.Equal("Large Section", chunks[0].Metadata.Section);
            // Subsequent chunks should have part numbers
            if (chunks.Count > 1)
                Assert.Contains("part", chunks[1].Metadata.Section);
            _output.WriteLine($"Large section split into {chunks.Count} sub-chunks");
        }

        [Fact]
        public void ChunkFromMarkdown_PreservesHeadingInSubChunks()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("## My Section");
            sb.AppendLine();
            for (int i = 0; i < 15; i++)
            {
                sb.AppendLine($"Line {i}: {new string('a', 150)}");
                sb.AppendLine();
            }

            var chunks = KnowledgeChunker.ChunkFromMarkdown(sb.ToString(), "test.md", tokenLimit: 80);

            // Each sub-chunk should start with the heading for context
            foreach (var chunk in chunks)
            {
                Assert.StartsWith("## My Section", chunk.Content);
            }
        }

        [Fact]
        public void ChunkFromMarkdown_EmptyInput_ReturnsEmpty()
        {
            var chunks = KnowledgeChunker.ChunkFromMarkdown("", "test.md", tokenLimit: 200);
            Assert.Empty(chunks);
        }

        [Fact]
        public void ChunkFromMarkdown_WhitespaceOnly_ReturnsEmpty()
        {
            var chunks = KnowledgeChunker.ChunkFromMarkdown("   \n\n   ", "test.md", tokenLimit: 200);
            Assert.Empty(chunks);
        }

        [Fact]
        public void ChunkFromMarkdown_PreservesSourceMetadata()
        {
            var markdown = "# Test\n\nContent.";
            var chunks = KnowledgeChunker.ChunkFromMarkdown(markdown, "document.md", tokenLimit: 500);

            Assert.Single(chunks);
            Assert.Equal("document.md", chunks[0].Metadata.Source);
            Assert.Equal("Test", chunks[0].Metadata.Section);
        }

        [Fact]
        public void ChunkFromMarkdown_GeneratesTags()
        {
            var markdown = "# Docker Deployment Guide\n\nContent.";
            var chunks = KnowledgeChunker.ChunkFromMarkdown(markdown, "test.md", tokenLimit: 500);

            Assert.Contains("docker", chunks[0].Metadata.Tags);
            Assert.Contains("deployment", chunks[0].Metadata.Tags);
            Assert.Contains("guide", chunks[0].Metadata.Tags);
        }

        [Fact]
        public void ChunkFromMarkdown_UsesSettingsTokenLimit()
        {
            // Override settings with small token limit
            SettingsProvider.Initialize(new ChatCompleteSettings
            {
                ChunkParagraphTokens = 50
            });

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("# Section");
            sb.AppendLine();
            for (int i = 0; i < 10; i++)
            {
                sb.AppendLine($"Paragraph {i} with enough text to exceed fifty tokens when combined. {new string('x', 100)}");
                sb.AppendLine();
            }

            // tokenLimit=0 means "use settings"
            var chunks = KnowledgeChunker.ChunkFromMarkdown(sb.ToString(), "test.md");

            Assert.True(chunks.Count > 1, "Should sub-chunk with small token limit from settings");

            // Reset settings
            SettingsProvider.Initialize(new ChatCompleteSettings
            {
                ChunkParagraphTokens = 200
            });
        }

        // ─── ChunkFromPlainText Tests ───

        [Fact]
        public void ChunkFromPlainText_SmallText_SingleChunk()
        {
            var text = "Some short plain text.";
            var chunks = KnowledgeChunker.ChunkFromPlainText(text, "notes.txt");

            Assert.Single(chunks);
            Assert.Equal("notes.txt", chunks[0].Metadata.Source);
            Assert.Equal("Merged Paragraphs", chunks[0].Metadata.Section);
        }

        [Fact]
        public void ChunkFromPlainText_LargeText_MultipleChunks()
        {
            SettingsProvider.Initialize(new ChatCompleteSettings
            {
                ChunkCharacterLimit = 100
            });

            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < 10; i++)
            {
                sb.AppendLine($"Paragraph {i}: {new string('y', 80)}");
                sb.AppendLine();
            }

            var chunks = KnowledgeChunker.ChunkFromPlainText(sb.ToString(), "large.txt");

            Assert.True(chunks.Count > 1);
            Assert.All(chunks, c => Assert.Equal("plain_text", c.Metadata.Tags[0]));

            // Reset
            SettingsProvider.Initialize(new ChatCompleteSettings
            {
                ChunkParagraphTokens = 200,
                ChunkCharacterLimit = 4096
            });
        }

        [Fact]
        public void ChunkFromPlainText_NullInput_ReturnsEmpty()
        {
            var chunks = KnowledgeChunker.ChunkFromPlainText(null, "test.txt");
            Assert.Empty(chunks);
        }

        [Fact]
        public void ChunkFromPlainText_EmptyInput_ReturnsEmpty()
        {
            var chunks = KnowledgeChunker.ChunkFromPlainText("", "test.txt");
            Assert.Empty(chunks);
        }

        // ─── EstimateTokenCount Tests ───

        [Fact]
        public void EstimateTokenCount_EmptyString_ReturnsZero()
        {
            Assert.Equal(0, KnowledgeChunker.EstimateTokenCount(""));
        }

        [Fact]
        public void EstimateTokenCount_NullString_ReturnsZero()
        {
            Assert.Equal(0, KnowledgeChunker.EstimateTokenCount(null!));
        }

        [Fact]
        public void EstimateTokenCount_FourChars_ReturnsOne()
        {
            Assert.Equal(1, KnowledgeChunker.EstimateTokenCount("abcd"));
        }

        [Fact]
        public void EstimateTokenCount_FiveChars_ReturnsTwo()
        {
            // ceil(5/4) = 2
            Assert.Equal(2, KnowledgeChunker.EstimateTokenCount("abcde"));
        }

        // ─── SubChunkSection Tests ───

        [Fact]
        public void SubChunkSection_SectionWithHeading_IncludesHeadingInEachChunk()
        {
            var section = new MarkdownSection
            {
                Title = "Test Section",
                Level = 2,
                Content = "## Test Section\n\n" + string.Join("\n\n",
                    Enumerable.Range(0, 10).Select(i => $"Paragraph {i}: {new string('z', 200)}"))
            };

            var chunks = KnowledgeChunker.SubChunkSection(section, "file.md", 80);

            Assert.True(chunks.Count > 1);
            foreach (var chunk in chunks)
            {
                Assert.StartsWith("## Test Section", chunk.Content);
            }
        }

        [Fact]
        public void SubChunkSection_NoLevel_OmitsHeadingPrefix()
        {
            var section = new MarkdownSection
            {
                Title = "Preamble",
                Level = 0,
                Content = string.Join("\n\n",
                    Enumerable.Range(0, 10).Select(i => $"Paragraph {i}: {new string('z', 200)}"))
            };

            var chunks = KnowledgeChunker.SubChunkSection(section, "file.md", 80);

            Assert.True(chunks.Count > 1);
            // Preamble chunks should NOT start with #
            foreach (var chunk in chunks)
            {
                Assert.False(chunk.Content.StartsWith("#"));
            }
        }

        // ─── Integration-style Tests ───

        [Fact]
        public void ChunkFromMarkdown_RealWorldDocument_ProducesReasonableChunks()
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
3. Run `npm install` in the webclient directory
4. Start the development server

## Configuration

Settings are stored in `appsettings.json`:

```json
{
  ""ChunkParagraphTokens"": 200,
  ""ChunkOverlap"": 40,
  ""ChunkCharacterLimit"": 4096
}
```

## Deployment

Deploy using Docker Compose for the easiest setup.
";

            var chunks = KnowledgeChunker.ChunkFromMarkdown(markdown, "README.md", tokenLimit: 500);

            _output.WriteLine($"Produced {chunks.Count} chunks from real-world document:");
            foreach (var chunk in chunks)
            {
                _output.WriteLine($"  [{chunk.Metadata.Section}] ({KnowledgeChunker.EstimateTokenCount(chunk.Content)} tokens)");
            }

            // Should produce multiple cohesive sections
            Assert.True(chunks.Count >= 3, $"Expected at least 3 sections, got {chunks.Count}");

            // Each chunk should have non-empty content and metadata
            Assert.All(chunks, c =>
            {
                Assert.False(string.IsNullOrWhiteSpace(c.Content));
                Assert.False(string.IsNullOrWhiteSpace(c.Metadata.Source));
                Assert.False(string.IsNullOrWhiteSpace(c.Metadata.Section));
                Assert.NotEmpty(c.Metadata.Tags);
            });
        }

        [Fact]
        public void ChunkFromMarkdown_VeryLargeDocument_DoesNotExplodeIntoTinyChunks()
        {
            // Simulate a document that previously caused 1000s of chunks with SemanticChunker
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("# Documentation");
            sb.AppendLine();
            for (int s = 0; s < 5; s++)
            {
                sb.AppendLine($"## Section {s}");
                sb.AppendLine();
                for (int p = 0; p < 10; p++)
                {
                    sb.AppendLine($"Content paragraph {p} in section {s}. This is a normal paragraph of text that contains information about the project and its features.");
                    sb.AppendLine();
                }
            }

            var chunks = KnowledgeChunker.ChunkFromMarkdown(sb.ToString(), "large-doc.md", tokenLimit: 200);

            _output.WriteLine($"Large document: {sb.Length} chars → {chunks.Count} chunks");

            // With 5 sections of ~10 paragraphs each at 200 token limit,
            // we should get roughly 5-20 chunks, NOT hundreds
            Assert.True(chunks.Count <= 50, $"Too many chunks ({chunks.Count}) — chunker is fragmenting too aggressively");
            Assert.True(chunks.Count >= 5, $"Too few chunks ({chunks.Count}) — sections should be separate");
        }
    }
}
