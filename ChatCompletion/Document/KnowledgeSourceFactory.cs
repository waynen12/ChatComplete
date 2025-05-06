public class KnowledgeSourceFactory
{
    private readonly List<IKnowledgeSource> _sources;

    public KnowledgeSourceFactory()
    {
        _sources = new List<IKnowledgeSource>
        {
            new DocxKnowledgeSource(),
            new MarkdownKnowledgeSource(),
            new PDFKnowledgeSource(),
            new TextKnowledgeSource()
        };
    }

    public IKnowledgeSource? GetSourceByExtension(string extension)
    {
        return _sources.FirstOrDefault(s =>
            s.SupportedFileExtension.Equals(extension, StringComparison.OrdinalIgnoreCase));
    }

    public IEnumerable<string> GetSupportedExtensions() =>
        _sources.Select(s => s.SupportedFileExtension);
}
