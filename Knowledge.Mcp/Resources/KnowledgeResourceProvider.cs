using Knowledge.Contracts;
using Knowledge.Data;
using Knowledge.Mcp.Resources.Models;
using System.Text.Json;
using Knowledge.Analytics.Services;
using KnowledgeEngine.Persistence;
using KnowledgeEngine.Services;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using System.ComponentModel;

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
[McpServerResourceType]
public class KnowledgeResourceProvider
{
    private readonly IKnowledgeRepository _knowledgeRepository;
    private readonly IUsageTrackingService _usageTrackingService;
    private readonly ISystemHealthService _systemHealthService;
    private readonly ILogger<KnowledgeResourceProvider> _logger;
    private readonly ResourceUriParser _uriParser;

    public KnowledgeResourceProvider(
        IKnowledgeRepository knowledgeRepository,
        IUsageTrackingService usageTrackingService,
        ISystemHealthService systemHealthService,
        ILogger<KnowledgeResourceProvider> logger)
    {
        _knowledgeRepository = knowledgeRepository ?? throw new ArgumentNullException(nameof(knowledgeRepository));
        _usageTrackingService = usageTrackingService ?? throw new ArgumentNullException(nameof(usageTrackingService));
        _systemHealthService = systemHealthService ?? throw new ArgumentNullException(nameof(systemHealthService));
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

        // 1. Add root collection list resource
        resources.Add(new ResourceMetadata
        {
            Uri = "resource://knowledge/collections",
            Name = "All Knowledge Collections",
            Description = "Complete list of knowledge bases with document counts and metadata",
            MimeType = "application/json"
        });

        // 2. Add system health resource
        resources.Add(new ResourceMetadata
        {
            Uri = "resource://system/health",
            Name = "System Health Status",
            Description = "Overall health metrics for vector stores, databases, and AI providers",
            MimeType = "application/json"
        });

        // 3. Add system models resource
        resources.Add(new ResourceMetadata
        {
            Uri = "resource://system/models",
            Name = "AI Model Inventory",
            Description = "List of available AI models (Ollama, OpenAI, Anthropic, Google)",
            MimeType = "application/json"
        });

        // 4. Query all collections and add resources for each
        var collections = await _knowledgeRepository.GetAllAsync(cancellationToken);

        foreach (var collection in collections)
        {
            // Add document list resource for each collection
            resources.Add(new ResourceMetadata
            {
                Uri = $"resource://knowledge/{collection.Id}/documents",
                Name = $"{collection.Name} - Documents",
                Description = $"List of {collection.DocumentCount} documents in {collection.Name}",
                MimeType = "application/json",
                Annotations = new Dictionary<string, object>
                {
                    ["collectionId"] = collection.Id,
                    ["documentCount"] = collection.DocumentCount
                }
            });

            // Add collection stats resource
            resources.Add(new ResourceMetadata
            {
                Uri = $"resource://knowledge/{collection.Id}/stats",
                Name = $"{collection.Name} - Statistics",
                Description = $"Analytics and usage statistics for {collection.Name}",
                MimeType = "application/json",
                Annotations = new Dictionary<string, object>
                {
                    ["collectionId"] = collection.Id
                }
            });

            // Get documents for each collection and add individual document resources
            var documents = await _knowledgeRepository.GetDocumentsByCollectionAsync(collection.Id, cancellationToken);
            foreach (var doc in documents)
            {
                // Detect MIME type from filename
                var fileName = doc.OriginalFileName.ToLowerInvariant();
                string mimeType = "text/plain";
                if (fileName.EndsWith(".md") || fileName.EndsWith(".markdown"))
                {
                    mimeType = "text/markdown";
                }
                else if (fileName.EndsWith(".json"))
                {
                    mimeType = "application/json";
                }

                resources.Add(new ResourceMetadata
                {
                    Uri = $"resource://knowledge/{collection.Id}/document/{doc.DocumentId}",
                    Name = doc.OriginalFileName,
                    Description = $"Document from {collection.Name} ({doc.ChunkCount} chunks, {doc.FileSize} bytes)",
                    MimeType = mimeType,
                    Annotations = new Dictionary<string, object>
                    {
                        ["collectionId"] = collection.Id,
                        ["documentId"] = doc.DocumentId,
                        ["chunkCount"] = doc.ChunkCount,
                        ["fileSize"] = doc.FileSize,
                        ["uploadedAt"] = doc.UploadedAt
                    }
                });
            }
        }

        _logger.LogInformation("MCP Resources: Returning {Count} resources ({CollectionCount} collections)",
            resources.Count, collections.Count());

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

        // Get all collections from repository
        var collections = await _knowledgeRepository.GetAllAsync(cancellationToken);

        // Build response object
        var response = new
        {
            collections = collections.Select(c => new
            {
                id = c.Id,
                name = c.Name,
                documentCount = c.DocumentCount
            }).ToList()
        };

        var resourceContent = CreateJsonResourceContent(
            "resource://knowledge/collections",
            response
        );

        return new ResourceReadResult
        {
            Contents = new[] { resourceContent }
        };
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

        // Verify collection exists
        var exists = await _knowledgeRepository.ExistsAsync(collectionId, cancellationToken);
        if (!exists)
        {
            throw new KeyNotFoundException($"Collection not found: {collectionId}");
        }

        // Get all documents for the collection
        var documents = await _knowledgeRepository.GetDocumentsByCollectionAsync(collectionId, cancellationToken);

        // Build response object
        var response = new
        {
            collectionId = collectionId,
            documents = documents.Select(d => new
            {
                id = d.DocumentId,
                fileName = d.OriginalFileName,
                fileType = d.FileType,
                chunkCount = d.ChunkCount,
                fileSize = d.FileSize,
                uploadedAt = d.UploadedAt
            }).ToList()
        };

        var resourceContent = CreateJsonResourceContent(
            $"resource://knowledge/{collectionId}/documents",
            response
        );

        return new ResourceReadResult
        {
            Contents = new[] { resourceContent }
        };
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

        // Verify collection exists
        var collectionExists = await _knowledgeRepository.ExistsAsync(collectionId, cancellationToken);
        if (!collectionExists)
        {
            throw new KeyNotFoundException($"Collection not found: {collectionId}");
        }

        // Get all chunks for the document
        var chunks = await _knowledgeRepository.GetDocumentChunksAsync(collectionId, documentId, cancellationToken);
        var chunkList = chunks.ToList();

        if (!chunkList.Any())
        {
            throw new KeyNotFoundException($"Document not found: {documentId} in collection {collectionId}");
        }

        // Reconstruct full document text from ordered chunks
        var fullText = string.Join("", chunkList.OrderBy(c => c.ChunkOrder).Select(c => c.ChunkText));

        // Get document metadata to determine file type
        var documents = await _knowledgeRepository.GetDocumentsByCollectionAsync(collectionId, cancellationToken);
        var documentMetadata = documents.FirstOrDefault(d => d.DocumentId == documentId);

        // Detect MIME type from file type/name
        string mimeType = "text/plain";
        if (documentMetadata != null)
        {
            var fileName = documentMetadata.OriginalFileName.ToLowerInvariant();
            if (fileName.EndsWith(".md") || fileName.EndsWith(".markdown"))
            {
                mimeType = "text/markdown";
            }
            else if (fileName.EndsWith(".json"))
            {
                mimeType = "application/json";
            }
            else if (fileName.EndsWith(".xml"))
            {
                mimeType = "application/xml";
            }
            else if (fileName.EndsWith(".html") || fileName.EndsWith(".htm"))
            {
                mimeType = "text/html";
            }
        }

        var resourceContent = CreateTextResourceContent(
            $"resource://knowledge/{collectionId}/document/{documentId}",
            fullText,
            mimeType
        );

        _logger.LogInformation(
            "Reconstructed document {DocumentId} from {ChunkCount} chunks ({CharCount} characters, MIME: {MimeType})",
            documentId,
            chunkList.Count,
            fullText.Length,
            mimeType
        );

        return new ResourceReadResult
        {
            Contents = new[] { resourceContent }
        };
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

        // Verify collection exists
        var exists = await _knowledgeRepository.ExistsAsync(collectionId, cancellationToken);
        if (!exists)
        {
            throw new KeyNotFoundException($"Collection not found: {collectionId}");
        }

        // Get collection metadata
        var collections = await _knowledgeRepository.GetAllAsync(cancellationToken);
        var collection = collections.FirstOrDefault(c => c.Id == collectionId);

        if (collection == null)
        {
            throw new KeyNotFoundException($"Collection not found: {collectionId}");
        }

        // Get usage statistics for this collection
        var allKnowledgeStats = await _usageTrackingService.GetKnowledgeUsageStatsAsync(cancellationToken);
        var usageStats = allKnowledgeStats.FirstOrDefault(k => k.KnowledgeId == collectionId);

        // Build response object
        var response = new
        {
            collectionId = collection.Id,
            name = collection.Name,
            documentCount = collection.DocumentCount,
            chunkCount = usageStats?.ChunkCount ?? 0,
            totalQueries = usageStats?.QueryCount ?? 0,
            conversationCount = usageStats?.ConversationCount ?? 0,
            lastQueried = usageStats?.LastQueried,
            createdAt = usageStats?.CreatedAt,
            vectorStore = usageStats?.VectorStore ?? "Qdrant",
            totalFileSize = usageStats?.TotalFileSize ?? 0
        };

        var resourceContent = CreateJsonResourceContent(
            $"resource://knowledge/{collectionId}/stats",
            response
        );

        return new ResourceReadResult
        {
            Contents = new[] { resourceContent }
        };
    }

    /// <summary>
    /// Reads current system health status.
    /// URI: resource://system/health
    /// </summary>
    private async Task<ResourceReadResult> ReadSystemHealthAsync(CancellationToken cancellationToken)
    {
        _logger.LogDebug("MCP Resources: Reading system health");

        // Get comprehensive system health from existing service
        var healthStatus = await _systemHealthService.GetSystemHealthAsync(cancellationToken);

        // Serialize the health status directly (SystemHealthStatus already has all needed properties)
        var resourceContent = CreateJsonResourceContent(
            "resource://system/health",
            healthStatus
        );

        return new ResourceReadResult
        {
            Contents = new[] { resourceContent }
        };
    }

    /// <summary>
    /// Reads the list of all available AI models with performance metrics.
    /// URI: resource://system/models
    /// </summary>
    private async Task<ResourceReadResult> ReadModelListAsync(CancellationToken cancellationToken)
    {
        _logger.LogDebug("MCP Resources: Reading model list");

        // Get model usage statistics from usage tracking service
        var modelStats = await _usageTrackingService.GetModelUsageStatsAsync(cancellationToken);
        var modelStatsList = modelStats.ToList();

        // Build models list from usage stats
        var models = modelStatsList.Select(m => new
        {
            name = m.ModelName,
            provider = m.Provider.ToString(),
            conversationCount = m.ConversationCount,
            totalTokens = m.TotalTokens,
            averageTokensPerRequest = m.AverageTokensPerRequest,
            averageResponseTime = m.AverageResponseTime.TotalMilliseconds,
            lastUsed = m.LastUsed,
            supportsTools = m.SupportsTools,
            successRate = m.SuccessRate,
            successfulRequests = m.SuccessfulRequests,
            failedRequests = m.FailedRequests
        }).ToList();

        // Build response object
        var response = new
        {
            totalModels = models.Count,
            providers = modelStatsList.Select(m => m.Provider.ToString()).Distinct().ToList(),
            models = models
        };

        var resourceContent = CreateJsonResourceContent(
            "resource://system/models",
            response
        );

        return new ResourceReadResult
        {
            Contents = new[] { resourceContent }
        };
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
