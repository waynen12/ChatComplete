using System.Text;
using System.Text.RegularExpressions;
using ChatCompletion.Config;

/// <summary>
/// Markdown-aware text chunker that splits documents along heading boundaries (h1/h2/h3),
/// handles preamble before the first heading, and sub-chunks oversized sections by paragraph
/// then by word. Each sub-chunk preserves the section heading for retrieval context.
/// </summary>
public static partial class KnowledgeChunker
{
    private const string FallbackSectionTitle = "Document";
    private const string PreambleSectionTitle = "Preamble";
    private const int FallbackTokenLimit = 200;

    private static readonly Regex HeadingRegex = CreateHeadingRegex();
    private static readonly Regex ParagraphSplitRegex = CreateParagraphSplitRegex();
    private static readonly Regex TokenRegex = CreateTokenRegex();

    public static List<KnowledgeChunk> ChunkFromMarkdown(
        string markdown,
        string sourceFileName,
        ChatCompleteSettings? settings = null)
    {
        if (string.IsNullOrWhiteSpace(markdown))
            return new List<KnowledgeChunk>();

        var tokenLimit = ResolveTokenLimit(settings);
        var sections = ParseMarkdownSections(markdown);
        var chunks = new List<KnowledgeChunk>();

        foreach (var section in sections)
        {
            chunks.AddRange(CreateChunksFromSection(section, sourceFileName, tokenLimit));
        }

        return chunks;
    }

    public static List<KnowledgeChunk> ChunkFromPlainText(
        string? text,
        string sourceFileName,
        ChatCompleteSettings? settings = null)
    {
        if (string.IsNullOrWhiteSpace(text))
            return new List<KnowledgeChunk>();

        var tokenLimit = ResolveTokenLimit(settings);
        var paragraphs = ParagraphSplitRegex
            .Split(text.Trim())
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .ToList();

        if (paragraphs.Count == 0)
            return new List<KnowledgeChunk>();

        return BuildParagraphChunks(
            heading: null,
            sectionTitle: FallbackSectionTitle,
            paragraphs,
            sourceFileName,
            tokenLimit);
    }

    internal static IEnumerable<MarkdownSection> ParseMarkdownSections(string markdown)
    {
        var normalized = markdown.Replace("\r\n", "\n").Trim();
        if (string.IsNullOrWhiteSpace(normalized))
            yield break;

        var matches = HeadingRegex.Matches(normalized);

        if (matches.Count == 0)
        {
            yield return new MarkdownSection(null, FallbackSectionTitle, normalized);
            yield break;
        }

        // Content before the first heading becomes the preamble
        if (matches[0].Index > 0)
        {
            var preamble = normalized[..matches[0].Index].Trim();
            if (!string.IsNullOrWhiteSpace(preamble))
                yield return new MarkdownSection(null, PreambleSectionTitle, preamble);
        }

        for (int i = 0; i < matches.Count; i++)
        {
            var match = matches[i];
            var nextIndex = i + 1 < matches.Count ? matches[i + 1].Index : normalized.Length;
            var sectionText = normalized[match.Index..nextIndex].Trim();
            if (string.IsNullOrWhiteSpace(sectionText))
                continue;

            var headingLine = match.Value.Trim();
            var title = headingLine.TrimStart('#').Trim();
            var body = ExtractSectionBody(sectionText, headingLine);
            yield return new MarkdownSection(headingLine, title, body);
        }
    }

    private static List<KnowledgeChunk> CreateChunksFromSection(
        MarkdownSection section,
        string sourceFileName,
        int tokenLimit)
    {
        var paragraphs = ParagraphSplitRegex
            .Split(section.Body)
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .ToList();

        if (paragraphs.Count == 0)
        {
            // Section with heading but no body, or empty body
            var content = string.IsNullOrWhiteSpace(section.HeadingLine)
                ? section.Body.Trim()
                : section.HeadingLine.Trim();

            if (string.IsNullOrWhiteSpace(content))
                return new List<KnowledgeChunk>();

            return new List<KnowledgeChunk>
            {
                CreateChunk(content, sourceFileName, section.Title)
            };
        }

        return BuildParagraphChunks(
            section.HeadingLine,
            section.Title,
            paragraphs,
            sourceFileName,
            tokenLimit);
    }

    private static List<KnowledgeChunk> BuildParagraphChunks(
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
            var normalized = paragraph.Trim();
            if (string.IsNullOrWhiteSpace(normalized))
                continue;

            // Check if adding this paragraph would exceed the limit
            var candidateParagraphs = currentParagraphs.Append(normalized).ToList();
            if (CountTokens(ComposeChunkContent(heading, candidateParagraphs)) <= tokenLimit)
            {
                currentParagraphs.Add(normalized);
                continue;
            }

            // Flush the current chunk if non-empty
            if (currentParagraphs.Count > 0)
            {
                chunks.Add(CreateChunk(
                    ComposeChunkContent(heading, currentParagraphs),
                    sourceFileName,
                    sectionTitle));
                currentParagraphs.Clear();
            }

            // Handle oversized paragraph (exceeds limit even alone with heading)
            if (CountTokens(ComposeChunkContent(heading, new[] { normalized })) > tokenLimit)
            {
                var subChunks = SplitOversizedParagraph(
                    heading, normalized, sourceFileName, sectionTitle, tokenLimit);
                // Emit all but the last sub-chunk; keep the last as a potential merge candidate
                chunks.AddRange(subChunks.Take(subChunks.Count - 1));

                var trailingChunk = subChunks.Last();
                var trailingBody = ExtractBodyWithoutHeading(trailingChunk.Content, heading);
                if (!string.IsNullOrWhiteSpace(trailingBody))
                    currentParagraphs.Add(trailingBody);
            }
            else
            {
                currentParagraphs.Add(normalized);
            }
        }

        if (currentParagraphs.Count > 0)
        {
            chunks.Add(CreateChunk(
                ComposeChunkContent(heading, currentParagraphs),
                sourceFileName,
                sectionTitle));
        }

        return chunks;
    }

    private static List<KnowledgeChunk> SplitOversizedParagraph(
        string? heading,
        string paragraph,
        string sourceFileName,
        string sectionTitle,
        int tokenLimit)
    {
        var availableTokens = GetAvailableBodyTokens(heading, tokenLimit);
        var pieces = SplitTextByTokens(paragraph, availableTokens);
        return pieces
            .Select(piece => CreateChunk(
                ComposeChunkContent(heading, new[] { piece }),
                sourceFileName,
                sectionTitle))
            .ToList();
    }

    internal static string ComposeChunkContent(string? heading, IEnumerable<string> paragraphs)
    {
        var body = string.Join("\n\n",
            paragraphs.Where(p => !string.IsNullOrWhiteSpace(p)).Select(p => p.Trim()));

        if (string.IsNullOrWhiteSpace(heading))
            return body.Trim();

        return string.IsNullOrWhiteSpace(body)
            ? heading.Trim()
            : $"{heading.Trim()}\n\n{body.Trim()}";
    }

    private static string ExtractBodyWithoutHeading(string chunkContent, string? heading)
    {
        if (string.IsNullOrWhiteSpace(heading))
            return chunkContent.Trim();

        var content = chunkContent.Trim();
        var normalizedHeading = heading.Trim();

        return content.StartsWith(normalizedHeading, StringComparison.Ordinal)
            ? content[normalizedHeading.Length..].Trim()
            : content;
    }

    private static string ExtractSectionBody(string sectionText, string headingLine)
    {
        return sectionText.StartsWith(headingLine, StringComparison.Ordinal)
            ? sectionText[headingLine.Length..].Trim()
            : sectionText.Trim();
    }

    private static List<string> SplitTextByTokens(string text, int tokenLimit)
    {
        var words = text.Split((char[])null!, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
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

            // Handle single words that exceed the token limit (very rare)
            if (CountTokens(word) > tokenLimit)
            {
                pieces.AddRange(SplitSingleWord(word, tokenLimit));
            }
            else
            {
                builder.Append(word);
            }
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

    internal static int CountTokens(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return 0;
        return TokenRegex.Matches(text).Count;
    }

    private static int GetAvailableBodyTokens(string? heading, int tokenLimit)
    {
        if (string.IsNullOrWhiteSpace(heading))
            return tokenLimit;
        return Math.Max(1, tokenLimit - CountTokens(heading));
    }

    private static int ResolveTokenLimit(ChatCompleteSettings? settings)
    {
        var effective = settings ?? SettingsProvider.Settings;
        return effective?.ChunkParagraphTokens > 0
            ? effective.ChunkParagraphTokens
            : FallbackTokenLimit;
    }

    internal static string[] GenerateTags(string title)
    {
        return title
            .ToLowerInvariant()
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(t => t.Trim('.', ',', ':', ';', '!', '?', '(', ')', '[', ']', '{', '}'))
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .Distinct(StringComparer.Ordinal)
            .ToArray();
    }

    [GeneratedRegex(@"^(#{1,3})\s+.+$", RegexOptions.Multiline)]
    private static partial Regex CreateHeadingRegex();

    [GeneratedRegex(@"\n\s*\n+", RegexOptions.Multiline)]
    private static partial Regex CreateParagraphSplitRegex();

    [GeneratedRegex(@"\S+")]
    private static partial Regex CreateTokenRegex();

    internal sealed record MarkdownSection(string? HeadingLine, string Title, string Body);
}
