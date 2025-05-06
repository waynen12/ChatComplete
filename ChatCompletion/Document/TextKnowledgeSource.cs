public class TextKnowledgeSource : IKnowledgeSource
{
    public string SupportedFileExtension => ".txt";

    public async Task<KnowledgeParseResult> ParseAsync(Stream fileStream)
    {
        try
        {
            using var reader = new StreamReader(fileStream);
            string text = await reader.ReadToEndAsync();

            var doc = new Document
            {
                Source = "Uploaded Text File",
                Tags = new List<string>()
            };

            foreach (var line in text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.TrimEntries))
            {
                if (!string.IsNullOrWhiteSpace(line))
                {
                    doc.Elements.Add(new ParagraphElement(line));
                }
            }
            return KnowledgeParseResult.Ok(doc);
        }
        catch (Exception ex)
        {
            return KnowledgeParseResult.Fail($"Error parsing text file: {ex.Message}");
        }
    }
}
