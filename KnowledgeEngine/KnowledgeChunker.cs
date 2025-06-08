using System.Text;
using System.Text.RegularExpressions;
using ChatCompletion.Config;
using Microsoft.SemanticKernel.Text;   


public static class KnowledgeChunker
{
    public static List<KnowledgeChunk> ChunkFromMarkdown(string markdown, string sourceFileName)
    {
        var chunks = new List<KnowledgeChunk>();
        var sections = Regex.Split(markdown, @"(?=^#{1,2}\s)", RegexOptions.Multiline);

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

    public static List<KnowledgeChunk> ChunkFromPlainText(string? text, string sourceFileName)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return new List<KnowledgeChunk>();
        }
        // Split by paragraphs or custom logic
        // ensure each chunk is <= SettingsProvider.Settings.ChunkCharacterLimit tokens
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
            if (currentChunk.Length + paragraph.Length > SettingsProvider.Settings.ChunkCharacterLimit)
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

    private static string[] GenerateTags(string title)
    {
        return title
            .ToLowerInvariant()
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Select(t => t.Trim().Replace(":", "").Replace(",", ""))
            .ToArray();
    }
}
