using System.Text;
using System.Text.RegularExpressions;
using ChatCompletion.Config;


public static class KnowledgeChunker
{
    private static readonly Regex HeadingPattern = new(@"^#{1,3}(?:\s+|$)", RegexOptions.Compiled);
    private static readonly Regex HeadingPrefixPattern = new(@"^#{1,6}\s*", RegexOptions.Compiled);
    private static readonly Regex TokenPattern = new(@"\S+", RegexOptions.Compiled);
    private static readonly Regex SlugNonAlphaNumericPattern = new(@"[^a-z0-9]+", RegexOptions.Compiled);

    public static List<KnowledgeChunk> ChunkFromMarkdown(
        string markdown,
        string sourceFileName,
        int? tokenLimit = null
    )
    {
        var chunks = new List<KnowledgeChunk>();
        if (string.IsNullOrWhiteSpace(markdown))
        {
            return chunks;
        }

        var maxTokens = ResolveTokenLimit(tokenLimit);
        var sections = ParseMarkdownSections(markdown);

        foreach (var section in sections)
        {
            var sectionChunks = SubChunkSection(section, sourceFileName, maxTokens);
            chunks.AddRange(sectionChunks);
        }

        return chunks;
    }

    public static List<KnowledgeChunk> ChunkFromPlainText(string? text, string sourceFileName)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return new List<KnowledgeChunk>();
        }

        var maxTokens = ResolveTokenLimit(tokenLimit: null);
        var chunks = new List<KnowledgeChunk>();
        var paragraphs = text.Split("\n\n").Where(p => !string.IsNullOrWhiteSpace(p));
        var currentChunk = new StringBuilder();
        var currentTokenCount = 0;
        var currentMetadata = new KnowledgeMetadata
        {
            Source = sourceFileName,
            Section = "Merged Paragraphs",
            Tags = new[] { "plain_text" }
        };

        foreach (var paragraph in paragraphs)
        {
            var paragraphText = paragraph.Trim();
            if (paragraphText.Length == 0)
            {
                continue;
            }

            var paragraphTokens = EstimateTokenCount(paragraphText);
            var separatorTokens = currentChunk.Length > 0 ? 1 : 0;

            if (
                currentChunk.Length > 0
                && currentTokenCount + paragraphTokens + separatorTokens > maxTokens
            )
            {
                chunks.Add(new KnowledgeChunk
                {
                    Content = currentChunk.ToString().Trim(),
                    Metadata = currentMetadata
                });
                currentChunk.Clear();
                currentTokenCount = 0;
            }

            if (currentChunk.Length > 0)
            {
                currentChunk.AppendLine();
                currentTokenCount += 1;
            }

            currentChunk.AppendLine(paragraphText);
            currentTokenCount += paragraphTokens;
        }

        if (currentChunk.Length > 0)
        {
            chunks.Add(new KnowledgeChunk
            {
                Content = currentChunk.ToString().Trim(),
                Metadata = currentMetadata
            });
        }

        return chunks;
    }

    private static List<MarkdownSection> ParseMarkdownSections(string markdown)
    {
        var sections = new List<MarkdownSection>();
        var normalized = markdown.Replace("\r\n", "\n");
        var lines = normalized.Split('\n');
        var currentSection = new StringBuilder();
        string currentTitle = "Introduction";

        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            if (IsHeadingBoundary(line))
            {
                if (currentSection.Length > 0)
                {
                    sections.Add(
                        new MarkdownSection
                        {
                            Title = currentTitle,
                            Content = currentSection.ToString().Trim(),
                        }
                    );
                    currentSection.Clear();
                }

                currentTitle = ExtractHeadingTitle(line);
            }

            currentSection.AppendLine(line);
        }

        if (currentSection.Length > 0)
        {
            sections.Add(
                new MarkdownSection
                {
                    Title = currentTitle,
                    Content = currentSection.ToString().Trim(),
                }
            );
        }

        if (sections.Count == 0 && !string.IsNullOrWhiteSpace(markdown))
        {
            sections.Add(
                new MarkdownSection
                {
                    Title = "Introduction",
                    Content = markdown.Trim(),
                }
            );
        }

        return sections;
    }

    private static List<KnowledgeChunk> SubChunkSection(
        MarkdownSection section,
        string sourceFileName,
        int maxTokens
    )
    {
        var sectionContent = section.Content?.Trim() ?? string.Empty;
        if (sectionContent.Length == 0)
        {
            return new List<KnowledgeChunk>();
        }

        if (EstimateTokenCount(sectionContent) <= maxTokens)
        {
            return new List<KnowledgeChunk>
            {
                new()
                {
                    Content = sectionContent,
                    Metadata = new KnowledgeMetadata
                    {
                        Source = sourceFileName,
                        Section = section.Title,
                        Tags = GenerateTags(section.Title),
                    }
                }
            };
        }

        var headingPrefix = BuildHeadingPrefix(section);
        var chunks = new List<KnowledgeChunk>();
        var paragraphs = sectionContent
            .Split("\n\n", StringSplitOptions.RemoveEmptyEntries)
            .Select(p => p.Trim())
            .Where(p => p.Length > 0)
            .ToList();

        var currentChunk = new StringBuilder();
        var currentTokenCount = 0;

        foreach (var paragraph in paragraphs)
        {
            var paragraphTokens = EstimateTokenCount(paragraph);
            var separatorTokens = currentChunk.Length > 0 ? 1 : 0;

            if (paragraphTokens > maxTokens)
            {
                if (currentChunk.Length > 0)
                {
                    chunks.Add(
                        new KnowledgeChunk
                        {
                            Content = FinalizeSectionChunk(currentChunk.ToString(), headingPrefix),
                            Metadata = new KnowledgeMetadata
                            {
                                Source = sourceFileName,
                                Section = section.Title,
                                Tags = GenerateTags(section.Title),
                            }
                        }
                    );
                    currentChunk.Clear();
                    currentTokenCount = 0;
                }

                foreach (var split in SplitTextByTokenLimit(paragraph, maxTokens))
                {
                    chunks.Add(
                        new KnowledgeChunk
                        {
                            Content = FinalizeSectionChunk(split, headingPrefix),
                            Metadata = new KnowledgeMetadata
                            {
                                Source = sourceFileName,
                                Section = section.Title,
                                Tags = GenerateTags(section.Title),
                            }
                        }
                    );
                }
                continue;
            }

            if (
                currentChunk.Length > 0
                && currentTokenCount + paragraphTokens + separatorTokens > maxTokens
            )
            {
                chunks.Add(
                    new KnowledgeChunk
                    {
                        Content = FinalizeSectionChunk(currentChunk.ToString(), headingPrefix),
                        Metadata = new KnowledgeMetadata
                        {
                            Source = sourceFileName,
                            Section = section.Title,
                            Tags = GenerateTags(section.Title),
                        }
                    }
                );
                currentChunk.Clear();
                currentTokenCount = 0;
            }

            if (currentChunk.Length > 0)
            {
                currentChunk.AppendLine();
                currentTokenCount += 1;
            }

            currentChunk.AppendLine(paragraph);
            currentTokenCount += paragraphTokens;
        }

        if (currentChunk.Length > 0)
        {
            chunks.Add(
                new KnowledgeChunk
                {
                    Content = FinalizeSectionChunk(currentChunk.ToString(), headingPrefix),
                    Metadata = new KnowledgeMetadata
                    {
                        Source = sourceFileName,
                        Section = section.Title,
                        Tags = GenerateTags(section.Title),
                    }
                }
            );
        }

        return chunks;
    }

    private static string FinalizeSectionChunk(string content, string headingPrefix)
    {
        var trimmedContent = content.Trim();
        if (trimmedContent.StartsWith("#", StringComparison.Ordinal))
        {
            return trimmedContent;
        }

        return $"{headingPrefix}\n\n{trimmedContent}".Trim();
    }

    private static string BuildHeadingPrefix(MarkdownSection section)
    {
        if (section.Content.StartsWith("#", StringComparison.Ordinal))
        {
            var firstLine = section
                .Content.Replace("\r\n", "\n")
                .Split('\n', StringSplitOptions.RemoveEmptyEntries)
                .FirstOrDefault();

            if (!string.IsNullOrWhiteSpace(firstLine))
            {
                return firstLine.Trim();
            }
        }

        return $"## {section.Title}";
    }

    private static bool IsHeadingBoundary(string line)
    {
        if (string.IsNullOrWhiteSpace(line))
        {
            return false;
        }

        var trimmed = line.TrimStart();
        return HeadingPattern.IsMatch(trimmed);
    }

    private static string ExtractHeadingTitle(string headingLine)
    {
        var title = HeadingPrefixPattern.Replace(headingLine.Trim(), string.Empty).Trim();
        return string.IsNullOrWhiteSpace(title) ? "Untitled Section" : title;
    }

    private static int EstimateTokenCount(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return 0;
        }

        return TokenPattern.Matches(text).Count;
    }

    private static IEnumerable<string> SplitTextByTokenLimit(string text, int maxTokens)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            yield break;
        }

        var words = TokenPattern.Matches(text).Select(match => match.Value).ToArray();
        if (words.Length == 0)
        {
            yield break;
        }

        var current = new StringBuilder();
        var tokenCount = 0;

        foreach (var word in words)
        {
            if (tokenCount >= maxTokens)
            {
                yield return current.ToString().Trim();
                current.Clear();
                tokenCount = 0;
            }

            if (current.Length > 0)
            {
                current.Append(' ');
            }

            current.Append(word);
            tokenCount++;
        }

        if (current.Length > 0)
        {
            yield return current.ToString().Trim();
        }
    }

    private static int ResolveTokenLimit(int? tokenLimit)
    {
        if (tokenLimit is > 0)
        {
            return tokenLimit.Value;
        }

        if (SettingsProvider.Settings?.ChunkParagraphTokens > 0)
        {
            return SettingsProvider.Settings.ChunkParagraphTokens;
        }

        return 200;
    }

    private sealed class MarkdownSection
    {
        public string Title { get; set; } = "Untitled Section";
        public string Content { get; set; } = string.Empty;
    }

    private static string[] GenerateTags(string title)
    {
        var cleanedTitle = title.Trim();
        var sectionTag = $"section-{Slugify(cleanedTitle)}";

        var wordTags = cleanedTitle
            .ToLowerInvariant()
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Select(t => t.Trim().Replace(":", "").Replace(",", ""))
            .Where(t => t.Length > 0);

        return wordTags.Append(sectionTag).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
    }

    private static string Slugify(string value)
    {
        var normalized = value.ToLowerInvariant();
        var slug = SlugNonAlphaNumericPattern.Replace(normalized, "-").Trim('-');
        return string.IsNullOrWhiteSpace(slug) ? "untitled" : slug;
    }
}
