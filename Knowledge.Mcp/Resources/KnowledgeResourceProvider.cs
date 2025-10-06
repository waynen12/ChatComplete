using Knowledge.Contracts;
using Knowledge.Data;
using Knowledge.Mcp.Resources.Models;
using System.Text.Json;
using Knowledge.Analytics.Services;
using KnowledgeEngine.Persistence;
using Microsoft.Extensions.Logging;

namespace Knowledge.Mcp.Resources;

/// <summary>
/// Provides MCP resource access to knowledge base documents and collections.
/// Resources are read-only data endpoints that clients can browse and read.
///
/// This class handles the MCP resources protocol:
/// - resources/list: Returns catalog of all available resources
/// - resources/read: Returns content of specific resource
/// - resources/subscribe: Enables notifications when resources change
/// </summary>
public class KnowledgeResourceProvider
{
    private readonly IKnowledgeRepository _knowledgeRepository;
    private readonly IUsageTrackingService _usageTrackingService;
    private readonly ILogger<KnowledgeResourceProvider> _logger;
    private readonly ResourceUriParser _uriParser;

    public KnowledgeResourceProvider(
        IKnowledgeRepository knowledgeRepository,
        IUsageTrackingService usageTrackingService,
        ILogger<KnowledgeResourceProvider> logger)
    {
        _knowledgeRepository = knowledgeRepository ?? throw new ArgumentNullException(nameof(knowledgeRepository));
        _usageTrackingService = usageTrackingService ?? throw new ArgumentNullException(nameof(usageTrackingService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _uriParser = new ResourceUriParser();
    }

    #region Public MCP Protocol Methods

    /// <summary>
    /// Lists all available resources (called when client sends resources/list).
    /// Returns catalog of all resources clients can read.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of all available resource URIs with metadata</returns>
    public async Task<ResourceListResult> ListResourcesAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("MCP Resources: Listing all available resources");

        var resources = new List<ResourceMetadata>();

        // TODO (Day 2): Implement resource listing
        // 1. Add root collection list resource
        // 2. Add system health resource
        // 3. Add system models resource
        // 4. Query all collections and add document list/stats resources for each
        // 5. Add individual document resources

        _logger.LogInformation("MCP Resources: Returning {Count} resources", resources.Count);

        return new ResourceListResult
        {
            Resources = resources,
            NextCursor = null // No pagination for now
        };
    }

    /// <summary>
    /// Reads the content of a specific resource (called when client sends resources/read).
    /// Routes to appropriate handler based on URI pattern.
    /// </summary>
    /// <param name="uri">Resource URI to read</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Resource content</returns>
    public async Task<ResourceReadResult> ReadResourceAsync(
        string uri,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("MCP Resources: Reading resource {Uri}", uri);

        // Parse URI to determine resource type
        var parsedUri = _uriParser.Parse(uri);

        // Route to appropriate handler based on resource type
        return parsedUri.Type switch
        {
            ResourceType.CollectionList => await ReadCollectionListAsync(cancellationToken),
            ResourceType.DocumentList => await ReadDocumentListAsync(parsedUri.CollectionId!, cancellationToken),
            ResourceType.Document => await ReadDocumentAsync(parsedUri.CollectionId!, parsedUri.DocumentId!, cancellationToken),
            ResourceType.CollectionStats => await ReadCollectionStatsAsync(parsedUri.CollectionId!, cancellationToken),
            ResourceType.SystemHealth => await ReadSystemHealthAsync(cancellationToken),
            ResourceType.ModelList => await ReadModelListAsync(cancellationToken),
            _ => throw new InvalidOperationException($"Unsupported resource type: {parsedUri.Type}")
        };
    }

    /// <summary>
    /// Subscribes to resource updates (called when client sends resources/subscribe).
    /// Client will be notified when this resource changes.
    /// </summary>
    /// <param name="uri">Resource URI to subscribe to</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public Task SubscribeToResourceAsync(string uri, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("MCP Resources: Client subscribed to resource {Uri}", uri);

        // TODO (Future): Implement subscription tracking and notifications
        // For now, just acknowledge the subscription
        // In a real implementation:
        // 1. Store subscription in a registry
        // 2. When resource changes, send notification to client via MCP protocol
        // 3. Handle unsubscribe requests

        return Task.CompletedTask;
    }

    #endregion

    #region Private Resource Handlers (TODO: Implement in Day 2)

    /// <summary>
    /// Reads the list of all knowledge base collections.
    /// URI: resource://knowledge/collections
    /// </summary>
    private async Task<ResourceReadResult> ReadCollectionListAsync(CancellationToken cancellationToken)
    {
        _logger.LogDebug("MCP Resources: Reading collection list");

        // TODO (Day 2): Implement
        // 1. Query all collections from IKnowledgeRepository
        // 2. Build JSON array with collection metadata
        // 3. Return as ResourceContent with mimeType: application/json

        throw new NotImplementedException("ReadCollectionListAsync will be implemented in Day 2");
    }

    /// <summary>
    /// Reads the list of all documents in a specific collection.
    /// URI: resource://knowledge/{collectionId}/documents
    /// </summary>
    private async Task<ResourceReadResult> ReadDocumentListAsync(
        string collectionId,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("MCP Resources: Reading document list for collection {CollectionId}", collectionId);

        // Validate collection ID
        if (!ResourceUriParser.IsValidCollectionId(collectionId))
        {
            throw new ArgumentException($"Invalid collection ID: {collectionId}", nameof(collectionId));
        }

        // TODO (Day 2): Implement
        // 1. Query documents for collection from IKnowledgeRepository
        // 2. Build JSON array with document metadata
        // 3. Return as ResourceContent with mimeType: application/json

        throw new NotImplementedException("ReadDocumentListAsync will be implemented in Day 2");
    }

    /// <summary>
    /// Reads the full content of a specific document.
    /// URI: resource://knowledge/{collectionId}/document/{docId}
    /// </summary>
    private async Task<ResourceReadResult> ReadDocumentAsync(
        string collectionId,
        string documentId,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug(
            "MCP Resources: Reading document {DocumentId} from collection {CollectionId}",
            documentId,
            collectionId
        );

        // Validate IDs
        if (!ResourceUriParser.IsValidCollectionId(collectionId))
        {
            throw new ArgumentException($"Invalid collection ID: {collectionId}", nameof(collectionId));
        }

        if (!ResourceUriParser.IsValidDocumentId(documentId))
        {
            throw new ArgumentException($"Invalid document ID: {documentId}", nameof(documentId));
        }

        // TODO (Day 2): Implement
        // 1. Query document from IKnowledgeRepository
        // 2. Get full document content (not chunks, but original document if available)
        // 3. Detect MIME type (markdown, text, json, etc.)
        // 4. Return as ResourceContent with appropriate mimeType

        throw new NotImplementedException("ReadDocumentAsync will be implemented in Day 2");
    }

    /// <summary>
    /// Reads statistics and analytics for a specific collection.
    /// URI: resource://knowledge/{collectionId}/stats
    /// </summary>
    private async Task<ResourceReadResult> ReadCollectionStatsAsync(
        string collectionId,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("MCP Resources: Reading stats for collection {CollectionId}", collectionId);

        // Validate collection ID
        if (!ResourceUriParser.IsValidCollectionId(collectionId))
        {
            throw new ArgumentException($"Invalid collection ID: {collectionId}", nameof(collectionId));
        }

        // TODO (Day 3): Implement
        // 1. Get collection metadata from IKnowledgeRepository
        // 2. Get usage statistics from IUsageTrackingService
        // 3. Build JSON object with stats
        // 4. Return as ResourceContent with mimeType: application/json

        throw new NotImplementedException("ReadCollectionStatsAsync will be implemented in Day 3");
    }

    /// <summary>
    /// Reads current system health status.
    /// URI: resource://system/health
    /// </summary>
    private async Task<ResourceReadResult> ReadSystemHealthAsync(CancellationToken cancellationToken)
    {
        _logger.LogDebug("MCP Resources: Reading system health");

        // TODO (Day 3): Implement
        // 1. Delegate to existing ISystemHealthService
        // 2. Serialize health status to JSON
        // 3. Return as ResourceContent with mimeType: application/json

        throw new NotImplementedException("ReadSystemHealthAsync will be implemented in Day 3");
    }

    /// <summary>
    /// Reads the list of all available AI models with performance metrics.
    /// URI: resource://system/models
    /// </summary>
    private async Task<ResourceReadResult> ReadModelListAsync(CancellationToken cancellationToken)
    {
        _logger.LogDebug("MCP Resources: Reading model list");

        // TODO (Day 3): Implement
        // 1. Get list of Ollama models (via API or service)
        // 2. Get performance metrics from IUsageTrackingService
        // 3. Combine data and serialize to JSON
        // 4. Return as ResourceContent with mimeType: application/json

        throw new NotImplementedException("ReadModelListAsync will be implemented in Day 3");
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Creates a ResourceContent object with JSON data.
    /// </summary>
    private ResourceContent CreateJsonResourceContent(string uri, object data)
    {
        var json = JsonSerializer.Serialize(data, new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        return new ResourceContent
        {
            Uri = uri,
            MimeType = "application/json",
            Text = json
        };
    }

    /// <summary>
    /// Creates a ResourceContent object with text data.
    /// </summary>
    private ResourceContent CreateTextResourceContent(string uri, string text, string mimeType = "text/plain")
    {
        return new ResourceContent
        {
            Uri = uri,
            MimeType = mimeType,
            Text = text
        };
    }

    #endregion
}
