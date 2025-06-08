public class KnowledgeChunk
{
    public string Id { get; set; } = Guid.NewGuid().ToString(); // Auto-generate if not set
    public string Content { get; set; } = string.Empty;
    public KnowledgeMetadata Metadata { get; set; } = new KnowledgeMetadata();
}