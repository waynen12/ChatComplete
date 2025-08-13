namespace KnowledgeEngine.Models;

/// <summary>
/// Represents a search result from the knowledge vector store
/// </summary>
public record KnowledgeSearchResult
{
    /// <summary>
    /// The text content of the search result chunk.
    /// </summary>
    public string Text { get; init; } = string.Empty;
    
    /// <summary>
    /// The source document or file name where this content originated.
    /// </summary>
    public string Source { get; init; } = string.Empty;
    
    /// <summary>
    /// The order/position of this chunk within the source document.
    /// </summary>
    public int ChunkOrder { get; init; }
    
    /// <summary>
    /// Tags or metadata associated with this search result.
    /// </summary>
    public string Tags { get; init; } = string.Empty;
    
    /// <summary>
    /// The similarity score indicating how well this result matches the search query.
    /// Higher scores indicate better matches.
    /// </summary>
    public double Score { get; init; }
}