namespace Knowledge.Contracts;

/// <summary>
/// Metadata for a document within a knowledge collection.
/// Used for listing documents and basic information without retrieving full content.
/// </summary>
public class DocumentMetadataDto
{
    /// <summary>
    /// Unique identifier for the document (GUID format)
    /// </summary>
    public required string DocumentId { get; set; }

    /// <summary>
    /// Original filename when uploaded (e.g., "docker-ssl-guide.md")
    /// </summary>
    public required string OriginalFileName { get; set; }

    /// <summary>
    /// Number of chunks this document was split into
    /// </summary>
    public int ChunkCount { get; set; }

    /// <summary>
    /// File size in bytes
    /// </summary>
    public long FileSize { get; set; }

    /// <summary>
    /// MIME type or file extension (e.g., "text/markdown", ".md", ".pdf")
    /// </summary>
    public required string FileType { get; set; }

    /// <summary>
    /// Timestamp when the document was uploaded
    /// </summary>
    public DateTime UploadedAt { get; set; }
}
