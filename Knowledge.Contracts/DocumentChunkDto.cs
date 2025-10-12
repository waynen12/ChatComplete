namespace Knowledge.Contracts;

/// <summary>
/// Represents a single chunk of a document with its text content and metadata.
/// Used to reconstruct full documents from chunked storage.
/// </summary>
public class DocumentChunkDto
{
    /// <summary>
    /// Unique identifier for this chunk (GUID format)
    /// </summary>
    public required string ChunkId { get; set; }

    /// <summary>
    /// The actual text content of this chunk
    /// </summary>
    public required string ChunkText { get; set; }

    /// <summary>
    /// Zero-based ordering index for chunk reconstruction (0, 1, 2, ...)
    /// </summary>
    public int ChunkOrder { get; set; }

    /// <summary>
    /// Number of tokens in this chunk (used for LLM context limits)
    /// </summary>
    public int TokenCount { get; set; }

    /// <summary>
    /// Number of characters in this chunk
    /// </summary>
    public int CharacterCount { get; set; }
}
