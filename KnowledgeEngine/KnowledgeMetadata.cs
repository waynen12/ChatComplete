public class KnowledgeMetadata
{
    public string Source { get; set; } = string.Empty; // e.g., filename or document ID
    public string Section { get; set; } = string.Empty; // e.g., "Overview", "Best Practices"
    public string[] Tags { get; set; } = Array.Empty<string>();
}