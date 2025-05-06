public interface IKnowledgeSource
{
    Task<KnowledgeParseResult> ParseAsync(Stream fileStream);
    string SupportedFileExtension { get; }
}
