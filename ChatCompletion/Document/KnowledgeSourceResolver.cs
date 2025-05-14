using Amazon.Runtime.Internal.Util;

public class KnowledgeSourceResolver
{
    private readonly KnowledgeSourceFactory _factory;

    public KnowledgeSourceResolver(KnowledgeSourceFactory factory)
    {
        _factory = factory;
    }

    public async Task<KnowledgeParseResult> ParseAsync(Stream fileStream, string fileName)
    {
        var extension = Path.GetExtension(fileName);

        if (string.IsNullOrWhiteSpace(extension))
        {
            return KnowledgeParseResult.Fail("File has no extension.");
        }

        var source = _factory.GetSourceByExtension(extension);

        if (source == null)
        {
            LoggerProvider.Logger.Error($"No knowledge source registered for extension '{extension}'. Supported extensions: {string.Join(", ", _factory.GetSupportedExtensions())}");
            return KnowledgeParseResult.Fail(
                $"No knowledge source registered for extension '{extension}'. Supported extensions: {string.Join(", ", _factory.GetSupportedExtensions())}");
        }

        return await source.ParseAsync(fileStream);
    }
}
