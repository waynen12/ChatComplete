using System.Text;
using System.Text.RegularExpressions;
using ChatCompletion.Config;

public static partial class KnowledgeChunker
{
    private const int FallbackTokenLimit = 200;
    private const string PreambleSectionTitle = "Introduction";
    private const string NoHeadingSectionTitle = "Document";

    private static readonly Regex HeadingRegex = CreateHeadingRegex();
    private static readonly Regex ParagraphSplitRegex = CreateParagraphSplitRegex();
    private static readonly Regex TokenCountRegex = CreateTokenCountRegex();

    /// <summary>
    /// Splits markdown into chunks along h1/h2/h3 heading boundaries.
    /// Sections that exceed <paramref name="maxTokens"/> are sub-chunked by paragraph,
    /// then by word if a single paragraph is oversized.
    /// Each sub-chunk has the section heading prepended for retrieval context.
    /// </summary>
    /// <param name="maxTokens">
    /// Token limit per chunk. Pass 0 (default) to read from <see cref="ChatCompleteSettings.ChunkParagraphTokens"/>.
    /// </param>
    public static List<KnowledgeChunk> ChunkFromMarkdown(
        string markdown,
        string sourceFileName,
        int maxTokens = 0)
    {
        if (string.IsNullOrWhiteSpace(markdown))
            return new List<KnowledgeChunk>();

        int tokenLimit = maxTokens > 0 ? maxTokens : ResolveTokenLimit();
        var sections = ParseSections(markdown);
        var chunks = new List<KnowledgeChunk>();

        foreach (var section in sections)
            chunks.AddRange(CreateChunksFromSection(section, sourceFileName, tokenLimit));

        return chunks;
    }

    /// <summary>
    /// Splits plain text into chunks respecting the configured token limit.
    /// </summary>
    public static List<KnowledgeChunk> ChunkFromPlainText(string? text, string sourceFileName)
    {
        if (string.IsNullOrWhiteSpace(text))
            return new List<KnowledgeChunk>();

        int tokenLimit = ResolveTokenLimit();
        var paragraphs = ParagraphSplitRegex
            .Split(text.Trim())
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .ToList();

        if (paragraphs.Count == 0)
            return new List<KnowledgeChunk>();

        return BuildChunksFromParagraphs(null, NoHeadingSectionTitle, paragraphs, sourceFileName, tokenLimit);
    }

    // ── Section parsing ──────────────────────────────────────────────────────────

    private static List<MarkdownSection> ParseSections(string markdown)
    {
        var normalized = markdown.Replace("\r\n", "\n").Trim();
        var sections = new List<MarkdownSection>();
        var matches = HeadingRegex.Matches(normalized);

        if (matches.Count == 0)
        {
            sections.Add(new MarkdownSection(null, NoHeadingSectionTitle, normalized));
            return sections;
        }

        // Content before the first heading becomes the preamble
        if (matches[0].Index > 0)
        {
            var preamble = normalized[..matches[0].Index].Trim();
            if (!string.IsNullOrWhiteSpace(preamble))
                sections.Add(new MarkdownSection(null, PreambleSectionTitle, preamble));
        }

        for (int i = 0; i < matches.Count; i++)
        {
            var match = matches[i];
            var nextIndex = i + 1 < matches.Count ? matches[i + 1].Index : normalized.Length;
            var sectionText = normalized.Substring(match.Index, nextIndex - match.Index).Trim();

            if (string.IsNullOrWhiteSpace(sectionText))
                continue;

            var headingLine = match.Value.Trim();
            var title = headingLine.TrimStart('#').Trim();
            var body = sectionText.StartsWith(headingLine, StringComparison.Ordinal)
                ? sectionText[headingLine.Length..].Trim()
                : sectionText.Trim();

            sections.Add(new MarkdownSection(headingLine, title, body));
        }

        return sections;
    }

    // ── Chunk building ───────────────────────────────────────────────────────────

    private static List<KnowledgeChunk> CreateChunksFromSection(
        MarkdownSection section, string sourceFileName, int tokenLimit)
    {
        var paragraphs = ParagraphSplitRegex
            .Split(section.Body)
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .ToList();

        if (paragraphs.Count == 0)
        {
            var content = section.HeadingLine ?? section.Title;
            if (string.IsNullOrWhiteSpace(content))
                return new List<KnowledgeChunk>();
            return new List<KnowledgeChunk> { CreateChunk(content, sourceFileName, section.Title) };
        }

        return BuildChunksFromParagraphs(
            section.HeadingLine, section.Title, paragraphs, sourceFileName, tokenLimit);
    }

    private static List<KnowledgeChunk> BuildChunksFromParagraphs(
        string? heading,
        string sectionTitle,
        IReadOnlyList<string> paragraphs,
        string sourceFileName,
        int tokenLimit)
    {
        var chunks = new List<KnowledgeChunk>();
        var currentParagraphs = new List<string>();

        foreach (var paragraph in paragraphs)
        {
            var p = paragraph.Trim();
            if (string.IsNullOrWhiteSpace(p))
                continue;

            // Check whether this paragraph still fits within the limit
            var candidate = currentParagraphs.Append(p).ToList();
            if (CountTokens(ComposeContent(heading, candidate)) <= tokenLimit)
            {
                currentParagraphs.Add(p);
                continue;
            }

            // Flush the accumulated paragraphs
            if (currentParagraphs.Count > 0)
            {
                chunks.Add(CreateChunk(ComposeContent(heading, currentParagraphs), sourceFileName, sectionTitle));
                currentParagraphs.Clear();
            }

            // Sub-chunk the oversized paragraph at word boundaries
            var pieces = SplitOversizedParagraph(heading, p, sourceFileName, sectionTitle, tokenLimit);

            // Emit all pieces except the last; put the last piece back into accumulator
            // so it can be merged with the next paragraph if space allows.
            chunks.AddRange(pieces.Take(pieces.Count - 1));
            if (pieces.Count > 0)
            {
                var trailingBody = ExtractBody(pieces[^1].Content, heading);
                if (!string.IsNullOrWhiteSpace(trailingBody))
                    currentParagraphs.Add(trailingBody);
            }
        }

        if (currentParagraphs.Count > 0)
            chunks.Add(CreateChunk(ComposeContent(heading, currentParagraphs), sourceFileName, sectionTitle));

        return chunks;
    }

    // ── Oversized-paragraph splitting ────────────────────────────────────────────

    private static List<KnowledgeChunk> SplitOversizedParagraph(
        string? heading, string paragraph, string sourceFileName, string sectionTitle, int tokenLimit)
    {
        // Reserve tokens for the heading so each sub-chunk stays within the limit
        int bodyLimit = string.IsNullOrWhiteSpace(heading)
            ? tokenLimit
            : Math.Max(1, tokenLimit - CountTokens(heading));

        var pieces = SplitByTokens(paragraph, bodyLimit);
        return pieces
            .Select(piece => CreateChunk(ComposeContent(heading, new[] { piece }), sourceFileName, sectionTitle))
            .ToList();
    }

    private static List<string> SplitByTokens(string text, int tokenLimit)
    {
        var words = TokenCountRegex.Matches(text).Select(m => m.Value).ToArray();
        if (words.Length == 0)
            return new List<string>();

        var pieces = new List<string>();
        var builder = new StringBuilder();

        foreach (var word in words)
        {
            var candidate = builder.Length == 0 ? word : $"{builder} {word}";
            if (CountTokens(candidate) <= tokenLimit)
            {
                if (builder.Length > 0) builder.Append(' ');
                builder.Append(word);
                continue;
            }

            if (builder.Length > 0)
            {
                pieces.Add(builder.ToString());
                builder.Clear();
            }

            // A single word that is itself too long — split it at character boundaries
            if (CountTokens(word) > tokenLimit)
                pieces.AddRange(SplitSingleWord(word, tokenLimit));
            else
                builder.Append(word);
        }

        if (builder.Length > 0)
            pieces.Add(builder.ToString());

        return pieces;
    }

    private static IEnumerable<string> SplitSingleWord(string word, int tokenLimit)
    {
        var current = new StringBuilder();
        foreach (var ch in word)
        {
            var candidate = current.Length == 0 ? ch.ToString() : $"{current}{ch}";
            if (CountTokens(candidate) <= tokenLimit)
            {
                current.Append(ch);
                continue;
            }
            if (current.Length > 0)
            {
                yield return current.ToString();
                current.Clear();
            }
            current.Append(ch);
        }
        if (current.Length > 0)
            yield return current.ToString();
    }

    // ── Helpers ──────────────────────────────────────────────────────────────────

    private static string ComposeContent(string? heading, IEnumerable<string> paragraphs)
    {
        var body = string.Join("\n\n",
            paragraphs.Where(p => !string.IsNullOrWhiteSpace(p)).Select(p => p.Trim()));
        if (string.IsNullOrWhiteSpace(heading))
            return body.Trim();
        return string.IsNullOrWhiteSpace(body)
            ? heading.Trim()
            : $"{heading.Trim()}\n\n{body.Trim()}";
    }

    private static string ExtractBody(string chunkContent, string? heading)
    {
        if (string.IsNullOrWhiteSpace(heading))
            return chunkContent.Trim();
        var content = chunkContent.Trim();
        var h = heading.Trim();
        if (!content.StartsWith(h, StringComparison.Ordinal))
            return content;
        return content[h.Length..].Trim();
    }

    private static KnowledgeChunk CreateChunk(string content, string sourceFileName, string sectionTitle)
    {
        return new KnowledgeChunk
        {
            Content = content.Trim(),
            Metadata = new KnowledgeMetadata
            {
                Source = sourceFileName,
                Section = sectionTitle,
                Tags = GenerateTags(sectionTitle)
            }
        };
    }

    private static int ResolveTokenLimit()
    {
        var configured = SettingsProvider.Settings?.ChunkParagraphTokens ?? 0;
        return configured > 0 ? configured : FallbackTokenLimit;
    }

    // ── Token counting (internal for testability) ─────────────────────────────────

    public static int CountTokens(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return 0;
        return TokenCountRegex.Count(text);
    }

    private static string[] GenerateTags(string title)
    {
        return title
            .ToLowerInvariant()
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Select(t => t.Trim('.', ',', ':', ';', '!', '?', '(', ')', '[', ']', '{', '}'))
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .Distinct(StringComparer.Ordinal)
            .ToArray();
    }

    // ── Source-generated regexes ─────────────────────────────────────────────────

    [GeneratedRegex(@"^#{1,3}\s+.+$", RegexOptions.Multiline)]
    private static partial Regex CreateHeadingRegex();

    [GeneratedRegex(@"\n\s*\n+", RegexOptions.Multiline)]
    private static partial Regex CreateParagraphSplitRegex();

    [GeneratedRegex(@"\S+")]
    private static partial Regex CreateTokenCountRegex();

    private sealed record MarkdownSection(string? HeadingLine, string Title, string Body);
}
