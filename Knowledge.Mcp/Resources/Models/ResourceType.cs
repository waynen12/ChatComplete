namespace Knowledge.Mcp.Resources.Models;

/// <summary>
/// Types of resources exposed by the MCP server.
/// Each type corresponds to a specific URI pattern and content format.
/// </summary>
public enum ResourceType
{
    /// <summary>
    /// List of all knowledge base collections.
    /// URI: resource://knowledge/collections
    /// Returns: JSON array of collection metadata
    /// </summary>
    CollectionList,

    /// <summary>
    /// List of all documents in a specific collection.
    /// URI: resource://knowledge/{collectionId}/documents
    /// Returns: JSON array of document metadata
    /// </summary>
    DocumentList,

    /// <summary>
    /// Full content of a specific document.
    /// URI: resource://knowledge/{collectionId}/document/{docId}
    /// Returns: Full document content (markdown, text, JSON, etc.)
    /// </summary>
    Document,

    /// <summary>
    /// Statistics and analytics for a specific collection.
    /// URI: resource://knowledge/{collectionId}/stats
    /// Returns: JSON object with document counts, usage stats, storage metrics
    /// </summary>
    CollectionStats,

    /// <summary>
    /// Current system health status.
    /// URI: resource://system/health
    /// Returns: JSON object with component health, metrics, recommendations
    /// </summary>
    SystemHealth,

    /// <summary>
    /// List of all available AI models.
    /// URI: resource://system/models
    /// Returns: JSON array of installed models with performance metrics
    /// </summary>
    ModelList
}
