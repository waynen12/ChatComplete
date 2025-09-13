using System.ComponentModel.DataAnnotations;

namespace KnowledgeEngine.Models;

/// <summary>
/// Record representing a knowledge chunk stored in the vector database
/// </summary>
public record KnowledgeChunkRecord
{
    /// <summary>
    /// Unique identifier for the chunk
    /// </summary>
    [Key]
    public string Id { get; init; } = string.Empty;
    
    /// <summary>
    /// Vector embedding of the text content (768 dimensions for text-embedding-ada-002)
    /// </summary>
    public ReadOnlyMemory<float> Vector { get; init; }
    
    /// <summary>
    /// The actual text content of the chunk
    /// </summary>
    public string Text { get; init; } = string.Empty;
    
    /// <summary>
    /// Source document or file name
    /// </summary>
    public string Source { get; init; } = string.Empty;
    
    /// <summary>
    /// Order of this chunk within the source document
    /// </summary>
    public int ChunkOrder { get; init; }
    
    /// <summary>
    /// Tags associated with this chunk (comma-separated)
    /// </summary>
    public string Tags { get; init; } = string.Empty;
}
