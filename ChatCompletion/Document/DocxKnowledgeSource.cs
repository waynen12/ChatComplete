

public class DocxKnowledgeSource : IKnowledgeSource
{
    public string SupportedFileExtension => ".docx";

    public async Task<KnowledgeParseResult> ParseAsync(Stream fileStream)
    {
        try
        {        
            var converter = new DocxToDocumentConverter();
            var doc = converter.Convert(fileStream, "Uploaded DOCX");
            return KnowledgeParseResult.Ok(doc);
        }
        catch(Exception ex)
        {
            LoggerProvider.Logger.Error($"Error parsing DOCX", ex);
            return KnowledgeParseResult.Fail($"Error parsing DOCX: {ex.Message}");
        }
        
    }
}
