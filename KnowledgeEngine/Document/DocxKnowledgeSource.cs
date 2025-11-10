using KnowledgeEngine.Logging;

public class DocxKnowledgeSource : IKnowledgeSource
{
    public string SupportedFileExtension => ".docx";

    public Task<KnowledgeParseResult> ParseAsync(Stream fileStream)
    {
        try
        {
            var converter = new DocxToDocumentConverter();
            var doc = converter.Convert(fileStream, "Uploaded DOCX");
            return Task.FromResult(KnowledgeParseResult.Ok(doc));
        }
        catch (Exception ex)
        {
            LoggerProvider.Logger.Error($"Error parsing DOCX", ex);
            return Task.FromResult(KnowledgeParseResult.Fail($"Error parsing DOCX: {ex.Message}"));
        }
    }
}
