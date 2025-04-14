using System.Text.RegularExpressions;

public static class KnowledgeChunker
{
    public static List<KnowledgeChunk> ChunkFromMarkdown(string markdown, string sourceFileName)
    {
        var chunks = new List<KnowledgeChunk>();
        var sections = Regex.Split(markdown, @"(?=^##\s)", RegexOptions.Multiline);

        foreach (var section in sections)
        {
            if (string.IsNullOrWhiteSpace(section)) continue;

            var lines = section.Split('\n');
            string title = lines[0].Replace("##", "").Trim();

            chunks.Add(new KnowledgeChunk
            {
                Content = section.Trim(),
                Metadata = new KnowledgeMetadata
                {
                    Source = sourceFileName,
                    Section = title,
                    Tags = GenerateTags(title)
                }
            });
        }

        return chunks;
    }

    public static List<KnowledgeChunk> ChunkFromPlainText(string text, string sourceFileName)
    {
        // Split by paragraphs or custom logic
        return text.Split("\n\n")
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .Select((paragraph, index) => new KnowledgeChunk
            {
                Content = paragraph.Trim(),
                Metadata = new KnowledgeMetadata
                {
                    Source = sourceFileName,
                    Section = $"Paragraph {index + 1}",
                    Tags = new[] { "plain_text" }
                }
            }).ToList();
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
