namespace KnowledgeEngine.Persistence.VectorStores;

internal static class ChunkKeyParser
{
    public static int ParseChunkOrder(string key)
    {
        if (!key.Contains("-p", StringComparison.Ordinal))
        {
            return 0;
        }

        var parts = key.Split("-p");
        return parts.Length > 1 && int.TryParse(parts[1], out var order) ? order : 0;
    }

    public static NormalizedChunkMetadata FlattenMetadataForStorage(
        string key,
        IVectorStoreStrategy.ChunkMetadata metadata
    )
    {
        var sourceValue = string.IsNullOrWhiteSpace(metadata.Source) ? key : metadata.Source;
        var sectionValue = metadata.Section ?? string.Empty;
        var tagsValue = metadata.Tags is { Length: > 0 } ? string.Join(",", metadata.Tags) : string.Empty;
        return new NormalizedChunkMetadata(sourceValue, sectionValue, tagsValue);
    }

    public sealed record NormalizedChunkMetadata(string Source, string Section, string Tags);
}
