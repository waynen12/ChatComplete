public class KnowledgeParseResult
{
    public bool Success { get; set; }
    public Document? Document { get; set; }
    public string? Error { get; }

    private KnowledgeParseResult(bool success, Document? document, string? error = null)
    {
        Success = success;
        Document = document;
        Error = error;
    }

    public static KnowledgeParseResult Ok(Document doc) => new KnowledgeParseResult(true, doc);

    public static KnowledgeParseResult Fail(string errorMessage) => new KnowledgeParseResult(false, null, errorMessage);

}