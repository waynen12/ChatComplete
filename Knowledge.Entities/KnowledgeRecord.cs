namespace Knowledge.Entities;

/// <summary>
/// Database record for knowledge collections
/// </summary>
public class KnowledgeCollectionRecord
{
    public string CollectionId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int DocumentCount { get; set; }
    public int ChunkCount { get; set; }
    public int TotalTokens { get; set; }
    public string? EmbeddingModel { get; set; }
    public string VectorStore { get; set; } = "Qdrant";
    public string Status { get; set; } = "Active";
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// Database record for knowledge documents
/// </summary>
public class KnowledgeDocumentRecord
{
    public string CollectionId { get; set; } = string.Empty;
    public string DocumentId { get; set; } = string.Empty;
    public string? OriginalFileName { get; set; }
    public long? FileSize { get; set; }
    public string? FileType { get; set; }
    public int ChunkCount { get; set; }
    public string ProcessingStatus { get; set; } = "Pending";
    public string? ErrorMessage { get; set; }
    public DateTime UploadedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
}

/// <summary>
/// Database record for knowledge chunks
/// </summary>
public class KnowledgeChunkRecord
{
    public string CollectionId { get; set; } = string.Empty;
    public string DocumentId { get; set; } = string.Empty;
    public string ChunkId { get; set; } = string.Empty;
    public string ChunkText { get; set; } = string.Empty;
    public int ChunkOrder { get; set; }
    public int? TokenCount { get; set; }
    public int? CharacterCount { get; set; }
    public bool VectorStored { get; set; }
    public DateTime CreatedAt { get; set; }
}