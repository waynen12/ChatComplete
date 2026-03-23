using System.Text;
using System.Text.RegularExpressions;
using ChatCompletion.Config;

public static partial class KnowledgeChunker
{
    private const string FallbackSectionTitle = "Document";
    private static readonly Regex HeadingRegex = CreateHeadingRegex();
    private static readonly Regex ParagraphSplitRegex = CreateParagraphSplitRegex();
    private static readonly Regex TokenSplitRegex = CreateTokenSplitRegex();

    public static List<KnowledgeChunk> ChunkFromMarkdown(
        string markdown,
        string sourceFileName,
        ChatCompleteSettings? settings = null
    )
    {
        if (string.IsNullOrWhiteSpace(markdown))
        {
            return new List<KnowledgeChunk>();
        }

        var effectiveSettings = settings ?? SettingsProvider.Settings;
        var tokenLimit = GetTokenLimit(effectiveSettings);
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
        ChatCompleteSettings? settings = null
    )
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return new List<KnowledgeChunk>();
        }

        var effectiveSettings = settings ?? SettingsProvider.Settings;
        var tokenLimit = GetTokenLimit(effectiveSettings);
        var paragraphs = ParagraphSplitRegex
            .Split(text.Trim())
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .ToList();

        if (paragraphs.Count == 0)
        {
            return new List<KnowledgeChunk>();
        }

        return BuildParagraphChunks(
            heading: null,
            sectionTitle: FallbackSectionTitle,
            paragraphs,
            sourceFileName,
            tokenLimit
        );
    }

    private static List<KnowledgeChunk> CreateChunksFromSection(
        MarkdownSection section,
        string sourceFileName,
        int tokenLimit
    )
    {
        var paragraphs = ParagraphSplitRegex
            .Split(section.Body)
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .ToList();

        if (paragraphs.Count == 0)
        {
            var content = string.IsNullOrWhiteSpace(section.HeadingLine)
                ? section.Body.Trim()
                : section.HeadingLine.Trim();

            if (string.IsNullOrWhiteSpace(content))
            {
                return new List<KnowledgeChunk>();
            }

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
            tokenLimit
        );
    }

    private static List<KnowledgeChunk> BuildParagraphChunks(
        string? heading,
        string sectionTitle,
        IReadOnlyList<string> paragraphs,
        string sourceFileName,
        int tokenLimit
    )
    {
        var chunks = new List<KnowledgeChunk>();
        var currentParagraphs = new List<string>();

        foreach (var paragraph in paragraphs)
        {
            var normalizedParagraph = paragraph.Trim();
            if (string.IsNullOrWhiteSpace(normalizedParagraph))
            {
                continue;
            }

            if (CountTokens(ComposeChunkContent(heading, currentParagraphs)) > tokenLimit)
            {
                throw new InvalidOperationException("Current chunk exceeded token limit before flush.");
            }

            var candidateParagraphs = currentParagraphs.Append(normalizedParagraph).ToList();
            if (CountTokens(ComposeChunkContent(heading, candidateParagraphs)) <= tokenLimit)
            {
                currentParagraphs.Add(normalizedParagraph);
                continue;
            }

            if (currentParagraphs.Count > 0)
            {
                chunks.Add(
                    CreateChunk(
                        ComposeChunkContent(heading, currentParagraphs),
                        sourceFileName,
                        sectionTitle
                    )
                );
                currentParagraphs.Clear();
            }

            var paragraphChunks = SplitOversizedParagraph(
                heading,
                normalizedParagraph,
                sourceFileName,
                sectionTitle,
                tokenLimit
            );
            chunks.AddRange(paragraphChunks.Take(paragraphChunks.Count - 1));

            var trailingChunk = paragraphChunks.Last();
            var trailingBody = ExtractBodyWithoutHeading(trailingChunk.Content, heading);
            if (!string.IsNullOrWhiteSpace(trailingBody))
            {
                currentParagraphs.Add(trailingBody);
            }
        }

        if (currentParagraphs.Count > 0)
        {
            chunks.Add(
                CreateChunk(
                    ComposeChunkContent(heading, currentParagraphs),
                    sourceFileName,
                    sectionTitle
                )
            );
        }

        return chunks;
    }

    private static List<KnowledgeChunk> SplitOversizedParagraph(
        string? heading,
        string paragraph,
        string sourceFileName,
        string sectionTitle,
        int tokenLimit
    )
    {
        var pieces = SplitTextByTokens(paragraph, GetAvailableBodyTokens(heading, tokenLimit));
        return pieces
            .Select(piece => CreateChunk(ComposeChunkContent(heading, new[] { piece }), sourceFileName, sectionTitle))
            .ToList();
    }

    private static IEnumerable<MarkdownSection> ParseMarkdownSections(string markdown)
    {
        var normalized = markdown.Replace("\r\n", "\n").Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            yield break;
        }

        var matches = HeadingRegex.Matches(normalized);
        if (matches.Count == 0)
        {
            yield return new MarkdownSection(null, FallbackSectionTitle, normalized);
            yield break;
        }

        if (matches[0].Index > 0)
        {
            var intro = normalized[..matches[0].Index].Trim();
            if (!string.IsNullOrWhiteSpace(intro))
            {
                yield return new MarkdownSection(null, FallbackSectionTitle, intro);
            }
        }

        for (int i = 0; i < matches.Count; i++)
        {
            var match = matches[i];
            var nextIndex = i + 1 < matches.Count ? matches[i + 1].Index : normalized.Length;
            var sectionText = normalized.Substring(match.Index, nextIndex - match.Index).Trim();
            if (string.IsNullOrWhiteSpace(sectionText))
            {
                continue;
            }

            var headingLine = match.Value.Trim();
            var title = headingLine.TrimStart('#').Trim();
            yield return new MarkdownSection(headingLine, title, ExtractSectionBody(sectionText, headingLine));
        }
    }

    private static string ExtractSectionBody(string sectionText, string headingLine)
    {
        if (!sectionText.StartsWith(headingLine, StringComparison.Ordinal))
        {
            return sectionText.Trim();
        }

        return sectionText[headingLine.Length..].Trim();
    }

    private static string ComposeChunkContent(string? heading, IEnumerable<string> paragraphs)
    {
        var body = string.Join("\n\n", paragraphs.Where(p => !string.IsNullOrWhiteSpace(p)).Select(p => p.Trim()));
        if (string.IsNullOrWhiteSpace(heading))
        {
            return body.Trim();
        }

        return string.IsNullOrWhiteSpace(body)
            ? heading.Trim()
            : $"{heading.Trim()}\n\n{body.Trim()}";
    }

    private static string ExtractBodyWithoutHeading(string chunkContent, string? heading)
    {
        if (string.IsNullOrWhiteSpace(heading))
        {
            return chunkContent.Trim();
        }

        var normalizedContent = chunkContent.Trim();
        var normalizedHeading = heading.Trim();

        if (!normalizedContent.StartsWith(normalizedHeading, StringComparison.Ordinal))
        {
            return normalizedContent;
        }

        return normalizedContent[normalizedHeading.Length..].Trim();
    }

    private static List<string> SplitTextByTokens(string text, int tokenLimit)
    {
        var words = text
            .Split((char[])null!, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();

        if (words.Count == 0)
        {
            return new List<string>();
        }

        var pieces = new List<string>();
        var builder = new StringBuilder();

        foreach (var word in words)
        {
            var candidate = builder.Length == 0 ? word : $"{builder} {word}";
            if (CountTokens(candidate) <= tokenLimit)
            {
                if (builder.Length > 0)
                {
                    builder.Append(' ');
                }
                builder.Append(word);
                continue;
            }

            if (builder.Length > 0)
            {
                pieces.Add(builder.ToString());
                builder.Clear();
            }

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
        {
            pieces.Add(builder.ToString());
        }

        return pieces;
    }

    private static IEnumerable<string> SplitSingleWord(string word, int tokenLimit)
    {
        var current = new StringBuilder();

        foreach (var character in word)
        {
            var candidate = current.Length == 0 ? character.ToString() : $"{current}{character}";
            if (CountTokens(candidate) <= tokenLimit)
            {
                current.Append(character);
                continue;
            }

            if (current.Length > 0)
            {
                yield return current.ToString();
                current.Clear();
            }

            current.Append(character);
        }

        if (current.Length > 0)
        {
            yield return current.ToString();
        }
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

    private static int GetTokenLimit(ChatCompleteSettings settings)
    {
        return settings.ChunkParagraphTokens > 0
            ? settings.ChunkParagraphTokens
            : 200;
    }

    private static int GetAvailableBodyTokens(string? heading, int tokenLimit)
    {
        if (string.IsNullOrWhiteSpace(heading))
        {
            return tokenLimit;
        }

        return Math.Max(1, tokenLimit - CountTokens(heading));
    }

    private static int CountTokens(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return 0;
        }

        return TokenSplitRegex.Matches(text).Count;
    }

    private static string[] GenerateTags(string title)
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
    private static partial Regex CreateTokenSplitRegex();

    private sealed record MarkdownSection(string? HeadingLine, string Title, string Body);
}
