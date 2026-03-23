using System.Text;
using System.Text.RegularExpressions;
using ChatCompletion.Config;

public static class KnowledgeChunker
{
    // Rough token estimate: ~4 chars per token for English text
    private const int CharsPerToken = 4;

    /// <summary>
    /// Chunks markdown text by splitting on heading boundaries (h1/h2/h3),
    /// keeping sections together, and sub-chunking sections that exceed the token limit.
    /// </summary>
    public static List<KnowledgeChunk> ChunkFromMarkdown(string markdown, string sourceFileName, int tokenLimit = 0)
    {
        if (string.IsNullOrWhiteSpace(markdown))
            return new List<KnowledgeChunk>();

        if (tokenLimit <= 0)
            tokenLimit = SettingsProvider.Settings?.ChunkParagraphTokens > 0
                ? SettingsProvider.Settings.ChunkParagraphTokens
                : 200;

        var chunks = new List<KnowledgeChunk>();
        var sections = SplitOnHeadings(markdown);

        foreach (var section in sections)
        {
            if (string.IsNullOrWhiteSpace(section.Content)) continue;

            int estimatedTokens = EstimateTokenCount(section.Content);

            if (estimatedTokens <= tokenLimit)
            {
                // Section fits within limit — keep it as one chunk
                chunks.Add(new KnowledgeChunk
                {
                    Content = section.Content.Trim(),
                    Metadata = new KnowledgeMetadata
                    {
                        Source = sourceFileName,
                        Section = section.Title,
                        Tags = GenerateTags(section.Title)
                    }
                });
            }
            else
            {
                // Section too large — sub-chunk by paragraphs
                var subChunks = SubChunkSection(section, sourceFileName, tokenLimit);
                chunks.AddRange(subChunks);
            }
        }

        return chunks;
    }

    /// <summary>
    /// Splits markdown into sections based on heading boundaries (h1/h2/h3).
    /// Each section includes its heading and all content until the next heading.
    /// </summary>
    internal static List<MarkdownSection> SplitOnHeadings(string markdown)
    {
        var sections = new List<MarkdownSection>();
        // Match lines starting with 1-3 # characters followed by a space
        var headingPattern = new Regex(@"^(#{1,3})\s+(.+)$", RegexOptions.Multiline);
        var matches = headingPattern.Matches(markdown);

        if (matches.Count == 0)
        {
            // No headings found — treat entire document as one section
            sections.Add(new MarkdownSection
            {
                Title = "Document",
                Level = 0,
                Content = markdown.Trim()
            });
            return sections;
        }

        // Content before the first heading (if any)
        int firstHeadingStart = matches[0].Index;
        if (firstHeadingStart > 0)
        {
            var preamble = markdown[..firstHeadingStart].Trim();
            if (!string.IsNullOrWhiteSpace(preamble))
            {
                sections.Add(new MarkdownSection
                {
                    Title = "Preamble",
                    Level = 0,
                    Content = preamble
                });
            }
        }

        // Process each heading and its content
        for (int i = 0; i < matches.Count; i++)
        {
            var match = matches[i];
            int level = match.Groups[1].Value.Length;
            string title = match.Groups[2].Value.Trim();

            int contentStart = match.Index;
            int contentEnd = i + 1 < matches.Count
                ? matches[i + 1].Index
                : markdown.Length;

            string content = markdown[contentStart..contentEnd].Trim();

            sections.Add(new MarkdownSection
            {
                Title = title,
                Level = level,
                Content = content
            });
        }

        return sections;
    }

    /// <summary>
    /// Sub-chunks a section that exceeds the token limit by splitting on paragraph boundaries.
    /// Each sub-chunk preserves the section heading as context.
    /// </summary>
    internal static List<KnowledgeChunk> SubChunkSection(
        MarkdownSection section, string sourceFileName, int tokenLimit)
    {
        var chunks = new List<KnowledgeChunk>();
        string headingLine = section.Level > 0
            ? $"{new string('#', section.Level)} {section.Title}\n\n"
            : "";

        // Remove the heading line from content for splitting
        string body = section.Content;
        if (section.Level > 0)
        {
            // Remove the first line (the heading) from the body
            int firstNewline = body.IndexOf('\n');
            if (firstNewline >= 0)
                body = body[(firstNewline + 1)..].TrimStart();
            else
                body = ""; // heading-only section
        }

        if (string.IsNullOrWhiteSpace(body))
            return chunks;

        // Split body into paragraphs (double newline) or individual lines
        var paragraphs = Regex.Split(body, @"\n{2,}")
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .ToList();

        int headingTokens = EstimateTokenCount(headingLine);
        int effectiveLimit = tokenLimit - headingTokens;
        if (effectiveLimit < 50) effectiveLimit = 50; // minimum useful size

        var currentChunk = new StringBuilder();
        int partIndex = 0;

        foreach (var paragraph in paragraphs)
        {
            int paragraphTokens = EstimateTokenCount(paragraph);

            // If a single paragraph exceeds the limit, split it by lines
            if (paragraphTokens > effectiveLimit)
            {
                // Flush current chunk first
                if (currentChunk.Length > 0)
                {
                    partIndex++;
                    chunks.Add(CreateSubChunk(
                        headingLine + currentChunk.ToString().Trim(),
                        sourceFileName, section.Title, partIndex));
                    currentChunk.Clear();
                }

                // Split the large paragraph by lines
                var lineChunks = SplitLargeParagraph(paragraph, effectiveLimit);
                foreach (var lineChunk in lineChunks)
                {
                    partIndex++;
                    chunks.Add(CreateSubChunk(
                        headingLine + lineChunk.Trim(),
                        sourceFileName, section.Title, partIndex));
                }
                continue;
            }

            int currentTokens = EstimateTokenCount(currentChunk.ToString());
            if (currentTokens + paragraphTokens > effectiveLimit && currentChunk.Length > 0)
            {
                // Flush and start new chunk
                partIndex++;
                chunks.Add(CreateSubChunk(
                    headingLine + currentChunk.ToString().Trim(),
                    sourceFileName, section.Title, partIndex));
                currentChunk.Clear();
            }

            currentChunk.AppendLine(paragraph);
            currentChunk.AppendLine(); // preserve paragraph separation
        }

        // Flush remaining content
        if (currentChunk.Length > 0)
        {
            partIndex++;
            chunks.Add(CreateSubChunk(
                headingLine + currentChunk.ToString().Trim(),
                sourceFileName, section.Title, partIndex));
        }

        return chunks;
    }

    /// <summary>
    /// Splits a single large paragraph into line-based chunks.
    /// </summary>
    private static List<string> SplitLargeParagraph(string paragraph, int tokenLimit)
    {
        var result = new List<string>();
        var lines = paragraph.Split('\n');
        var current = new StringBuilder();

        foreach (var line in lines)
        {
            int lineTokens = EstimateTokenCount(line);
            int currentTokens = EstimateTokenCount(current.ToString());

            if (currentTokens + lineTokens > tokenLimit && current.Length > 0)
            {
                result.Add(current.ToString());
                current.Clear();
            }

            current.AppendLine(line);
        }

        if (current.Length > 0)
            result.Add(current.ToString());

        return result;
    }

    private static KnowledgeChunk CreateSubChunk(
        string content, string sourceFileName, string sectionTitle, int partIndex)
    {
        return new KnowledgeChunk
        {
            Content = content,
            Metadata = new KnowledgeMetadata
            {
                Source = sourceFileName,
                Section = partIndex > 1 ? $"{sectionTitle} (part {partIndex})" : sectionTitle,
                Tags = GenerateTags(sectionTitle)
            }
        };
    }

    public static List<KnowledgeChunk> ChunkFromPlainText(string? text, string sourceFileName)
    {
        if (string.IsNullOrWhiteSpace(text))
            return new List<KnowledgeChunk>();

        int charLimit = SettingsProvider.Settings?.ChunkCharacterLimit > 0
            ? SettingsProvider.Settings.ChunkCharacterLimit
            : 4096;

        var chunks = new List<KnowledgeChunk>();
        var paragraphs = text.Split("\n\n").Where(p => !string.IsNullOrWhiteSpace(p));
        var currentChunk = new StringBuilder();
        var currentMetadata = new KnowledgeMetadata
        {
            Source = sourceFileName,
            Section = "Merged Paragraphs",
            Tags = new[] { "plain_text" }
        };

        foreach (var paragraph in paragraphs)
        {
            if (currentChunk.Length + paragraph.Length > charLimit)
            {
                chunks.Add(new KnowledgeChunk
                {
                    Content = currentChunk.ToString().Trim(),
                    Metadata = currentMetadata
                });
                currentChunk.Clear();
            }
            currentChunk.AppendLine(paragraph);
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

    /// <summary>
    /// Estimates token count from text length.
    /// Uses ~4 characters per token as a rough approximation for English text.
    /// </summary>
    internal static int EstimateTokenCount(string text)
    {
        if (string.IsNullOrEmpty(text)) return 0;
        return (int)Math.Ceiling((double)text.Length / CharsPerToken);
    }

    private static string[] GenerateTags(string title)
    {
        return title
            .ToLowerInvariant()
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Select(t => t.Trim().Replace(":", "").Replace(",", ""))
            .ToArray();
    }
}

/// <summary>
/// Represents a section of markdown content split by headings.
/// </summary>
public class MarkdownSection
{
    public string Title { get; set; } = string.Empty;
    public int Level { get; set; }
    public string Content { get; set; } = string.Empty;
}
