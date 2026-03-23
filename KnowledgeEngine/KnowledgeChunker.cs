using System.Text;
using System.Text.RegularExpressions;
using ChatCompletion.Config;

public static class KnowledgeChunker
{
    private static readonly Regex HeadingPattern =
        new(@"^(#{1,3})\s+(.+)$", RegexOptions.Multiline | RegexOptions.Compiled);

    /// <summary>
    /// Splits markdown into chunks along heading boundaries (h1/h2/h3).
    /// Sections that exceed <paramref name="maxTokens"/> are sub-chunked by paragraph.
    /// </summary>
    public static List<KnowledgeChunk> ChunkFromMarkdown(
        string markdown,
        string sourceFileName,
        int maxTokens = 0)
    {
        if (string.IsNullOrWhiteSpace(markdown))
            return new List<KnowledgeChunk>();

        if (maxTokens <= 0)
            maxTokens = ResolveTokenLimit();

        var sections = SplitByHeadings(markdown);
        var chunks = new List<KnowledgeChunk>();

        foreach (var section in sections)
        {
            if (string.IsNullOrWhiteSpace(section.Content))
                continue;

            int tokenCount = EstimateTokens(section.Content);

            if (tokenCount <= maxTokens)
            {
                chunks.Add(CreateChunk(section.Content, sourceFileName, section.Title));
            }
            else
            {
                var subChunks = SubChunkByParagraph(section.Content, section.Title, sourceFileName, maxTokens);
                chunks.AddRange(subChunks);
            }
        }

        return chunks;
    }

    public static List<KnowledgeChunk> ChunkFromPlainText(string? text, string sourceFileName)
    {
        if (string.IsNullOrWhiteSpace(text))
            return new List<KnowledgeChunk>();

        int maxTokens = ResolveTokenLimit();
        var chunks = new List<KnowledgeChunk>();
        var paragraphs = text.Split("\n\n").Where(p => !string.IsNullOrWhiteSpace(p));
        var currentChunk = new StringBuilder();

        foreach (var paragraph in paragraphs)
        {
            if (EstimateTokens(currentChunk.ToString() + paragraph) > maxTokens && currentChunk.Length > 0)
            {
                chunks.Add(CreateChunk(currentChunk.ToString().Trim(), sourceFileName, "Merged Paragraphs"));
                currentChunk.Clear();
            }
            currentChunk.AppendLine(paragraph);
        }

        if (currentChunk.Length > 0)
        {
            chunks.Add(CreateChunk(currentChunk.ToString().Trim(), sourceFileName, "Merged Paragraphs"));
        }

        return chunks;
    }

    /// <summary>
    /// Splits markdown text into sections based on heading boundaries.
    /// Content before the first heading becomes a section titled after the source or "Introduction".
    /// </summary>
    internal static List<MarkdownSection> SplitByHeadings(string markdown)
    {
        var sections = new List<MarkdownSection>();
        var matches = HeadingPattern.Matches(markdown);

        if (matches.Count == 0)
        {
            sections.Add(new MarkdownSection("Document", markdown.Trim(), 0));
            return sections;
        }

        // Content before the first heading
        int firstHeadingPos = matches[0].Index;
        if (firstHeadingPos > 0)
        {
            var preamble = markdown[..firstHeadingPos].Trim();
            if (!string.IsNullOrWhiteSpace(preamble))
            {
                sections.Add(new MarkdownSection("Introduction", preamble, 0));
            }
        }

        for (int i = 0; i < matches.Count; i++)
        {
            var match = matches[i];
            int level = match.Groups[1].Value.Length;
            string title = match.Groups[2].Value.Trim();

            int start = match.Index;
            int end = (i + 1 < matches.Count) ? matches[i + 1].Index : markdown.Length;
            string content = markdown[start..end].Trim();

            sections.Add(new MarkdownSection(title, content, level));
        }

        return sections;
    }

    /// <summary>
    /// Sub-chunks a section that exceeds the token limit by splitting on paragraph boundaries.
    /// Each sub-chunk gets the heading prepended for context and a part suffix in metadata.
    /// </summary>
    internal static List<KnowledgeChunk> SubChunkByParagraph(
        string sectionContent,
        string sectionTitle,
        string sourceFileName,
        int maxTokens)
    {
        var chunks = new List<KnowledgeChunk>();
        var lines = sectionContent.Split('\n');

        // Separate the heading line (if present) from the body
        string headingLine = "";
        int bodyStart = 0;
        if (lines.Length > 0 && HeadingPattern.IsMatch(lines[0]))
        {
            headingLine = lines[0];
            bodyStart = 1;
        }

        string body = string.Join('\n', lines[bodyStart..]).Trim();
        var paragraphs = Regex.Split(body, @"\n{2,}").Where(p => !string.IsNullOrWhiteSpace(p)).ToArray();

        var currentChunk = new StringBuilder();
        int headingTokens = EstimateTokens(headingLine);
        int partNumber = 1;

        foreach (var paragraph in paragraphs)
        {
            int candidateTokens = EstimateTokens(currentChunk.ToString() + "\n\n" + paragraph) + headingTokens;

            if (candidateTokens > maxTokens && currentChunk.Length > 0)
            {
                chunks.Add(CreateSubChunk(headingLine, currentChunk.ToString().Trim(), sourceFileName, sectionTitle, partNumber));
                currentChunk.Clear();
                partNumber++;
            }

            if (currentChunk.Length > 0)
                currentChunk.Append("\n\n");
            currentChunk.Append(paragraph);
        }

        if (currentChunk.Length > 0)
        {
            // If only one part, no need for part suffix
            string title = partNumber == 1 ? sectionTitle : sectionTitle;
            chunks.Add(CreateSubChunk(headingLine, currentChunk.ToString().Trim(), sourceFileName, title, partNumber));
        }

        // Edge case: a single paragraph that exceeds the token limit — still emit it as one chunk
        if (chunks.Count == 0 && !string.IsNullOrWhiteSpace(sectionContent))
        {
            chunks.Add(CreateChunk(sectionContent, sourceFileName, sectionTitle));
        }

        return chunks;
    }

    /// <summary>
    /// Rough token estimation: ~4 characters per token (GPT-family heuristic).
    /// </summary>
    internal static int EstimateTokens(string text)
    {
        if (string.IsNullOrEmpty(text)) return 0;
        return (int)Math.Ceiling(text.Length / 4.0);
    }

    private static KnowledgeChunk CreateChunk(string content, string source, string section)
    {
        return new KnowledgeChunk
        {
            Content = content,
            Metadata = new KnowledgeMetadata
            {
                Source = source,
                Section = section,
                Tags = GenerateTags(section)
            }
        };
    }

    private static KnowledgeChunk CreateSubChunk(
        string headingLine,
        string body,
        string source,
        string section,
        int partNumber)
    {
        // Prepend the heading so each sub-chunk has context
        string content = string.IsNullOrWhiteSpace(headingLine)
            ? body
            : $"{headingLine}\n\n{body}";

        return new KnowledgeChunk
        {
            Content = content,
            Metadata = new KnowledgeMetadata
            {
                Source = source,
                Section = $"{section} (part {partNumber})",
                Tags = GenerateTags(section)
            }
        };
    }

    private static int ResolveTokenLimit()
    {
        int configured = SettingsProvider.Settings.ChunkParagraphTokens;
        return configured > 0 ? configured : 200;
    }

    private static string[] GenerateTags(string title)
    {
        return title
            .ToLowerInvariant()
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Select(t => t.Trim().Replace(":", "").Replace(",", ""))
            .Where(t => t.Length > 0)
            .ToArray();
    }

    /// <summary>
    /// Represents a section extracted from markdown by heading boundaries.
    /// </summary>
    internal record MarkdownSection(string Title, string Content, int HeadingLevel);
}
