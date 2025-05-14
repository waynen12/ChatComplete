public class MarkdownKnowledgeSource : IKnowledgeSource
{
    public string SupportedFileExtension => ".md";

    public async Task<KnowledgeParseResult> ParseAsync(Stream fileStream)
    {
        try
        {                       
            using var reader = new StreamReader(fileStream);
            string markdownText = await reader.ReadToEndAsync();

            var converter = new MarkdownToDocumentConverter();
            var doc = converter.Convert(markdownText, "Uploaded Markdown");
            return KnowledgeParseResult.Ok(doc);
        }
        catch (Exception ex)
        {
            LoggerProvider.Logger.Error($"Error parsing markdown", ex);
            return KnowledgeParseResult.Fail($"Error parsing markdown: {ex.Message}");
        }

    }
}
