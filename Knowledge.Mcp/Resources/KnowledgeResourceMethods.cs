using System.ComponentModel;
using System.Text.Json;
using Knowledge.Analytics.Services;
using Knowledge.Contracts;
using KnowledgeEngine.Persistence;
using KnowledgeEngine.Services;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace Knowledge.Mcp.Resources;

/// <summary>
/// MCP Resource methods for knowledge base access.
/// Resources are registered via .WithResources<KnowledgeResourceMethods>().
/// Dependencies are injected via method parameters using IServiceProvider.
/// </summary>
[McpServerResourceType]
public class KnowledgeResourceMethods
{
    /// <summary>
    /// Lists all knowledge collections with metadata and document counts.
    /// URI: resource://knowledge/collections
    /// </summary>
    [
        McpServerResource(
            UriTemplate = "resource://knowledge/collections",
            Name = "Knowledge Collections",
            MimeType = "application/json"
        ),
        Description("Complete list of all knowledge collections with document counts and metadata")
    ]
    public static async Task<ResourceContents> GetCollections(
        RequestContext<ReadResourceRequestParams> requestContext,
        IKnowledgeRepository repository,
        ILogger<IKnowledgeRepository> logger,
        CancellationToken cancellationToken = default
    )
    {
        logger.LogInformation("MCP Resource: GetCollections");

        var collections = await repository.GetAllAsync(cancellationToken);

        var response = new
        {
            totalCollections = collections.Count(),
            collections = collections.Select(c => new
            {
                id = c.Id,
                name = c.Name,
                documentCount = c.DocumentCount,
            }),
        };

        var jsonText = JsonSerializer.Serialize(
            response,
            new JsonSerializerOptions { WriteIndented = true }
        );

        return new TextResourceContents
        {
            Uri = requestContext.Params?.Uri ?? "resource://knowledge/collections",
            MimeType = "application/json",
            Text = jsonText,
        };
    }

    /// <summary>
    /// Lists all documents in a specific knowledge collection.
    /// URI: resource://knowledge/{collectionId}/documents
    /// </summary>
    [
        McpServerResource(
            UriTemplate = "resource://knowledge/{collectionId}/documents",
            Name = "Collection Documents",
            MimeType = "application/json"
        ),
        Description("List of documents in a specific knowledge collection")
    ]
    public static async Task<ResourceContents> GetCollectionDocuments(
        RequestContext<ReadResourceRequestParams> requestContext,
        string collectionId,
        IKnowledgeRepository repository,
        ILogger<IKnowledgeRepository> logger,
        CancellationToken cancellationToken = default
    )
    {
        logger.LogInformation(
            "MCP Resource: GetCollectionDocuments for {CollectionId}",
            collectionId
        );

        // Validate collection exists
        var exists = await repository.ExistsAsync(collectionId, cancellationToken);
        if (!exists)
            throw new KeyNotFoundException($"Collection not found: {collectionId}");

        var documents = await repository.GetDocumentsByCollectionAsync(
            collectionId,
            cancellationToken
        );

        var response = new
        {
            collectionId,
            totalDocuments = documents.Count(),
            documents = documents.Select(d => new
            {
                documentId = d.DocumentId,
                fileName = d.OriginalFileName,
                chunkCount = d.ChunkCount,
                fileSize = d.FileSize,
                fileType = d.FileType,
                uploadedAt = d.UploadedAt,
            }),
        };

        var jsonText = JsonSerializer.Serialize(
            response,
            new JsonSerializerOptions { WriteIndented = true }
        );

        return new TextResourceContents
        {
            Uri = requestContext.Params?.Uri ?? $"resource://knowledge/{collectionId}/documents",
            MimeType = "application/json",
            Text = jsonText,
        };
    }

    /// <summary>
    /// Retrieves the full content of a specific document with automatic MIME type detection.
    /// URI: resource://knowledge/{collectionId}/document/{documentId}
    /// </summary>
    [
        McpServerResource(
            UriTemplate = "resource://knowledge/{collectionId}/document/{documentId}",
            Name = "Document Content",
            MimeType = "application/json"
        ),
        Description("Full content of a specific document with MIME type detection")
    ]
    public static async Task<ResourceContents> GetDocument(
        RequestContext<ReadResourceRequestParams> requestContext,
        string collectionId,
        string documentId,
        IKnowledgeRepository repository,
        ILogger<IKnowledgeRepository> logger,
        CancellationToken cancellationToken = default
    )
    {
        logger.LogInformation(
            "MCP Resource: GetDocument {CollectionId}/{DocumentId}",
            collectionId,
            documentId
        );

        // Validate collection exists
        var exists = await repository.ExistsAsync(collectionId, cancellationToken);
        if (!exists)
            throw new KeyNotFoundException($"Collection not found: {collectionId}");

        // Get all chunks for the document
        var chunks = await repository.GetDocumentChunksAsync(
            collectionId,
            documentId,
            cancellationToken
        );
        var chunkList = chunks.ToList();

        if (!chunkList.Any())
            throw new KeyNotFoundException(
                $"Document not found: {documentId} in collection {collectionId}"
            );

        // Reconstruct full document text from ordered chunks
        var fullText = string.Join(
            "",
            chunkList.OrderBy(c => c.ChunkOrder).Select(c => c.ChunkText)
        );

        // Get document metadata to determine file type
        var documents = await repository.GetDocumentsByCollectionAsync(
            collectionId,
            cancellationToken
        );
        var metadata = documents.FirstOrDefault(d => d.DocumentId == documentId);

        // Build response with metadata and content
        var response = new
        {
            collectionId,
            documentId,
            fileName = metadata?.OriginalFileName ?? "unknown",
            fileType = metadata?.FileType ?? "text/plain",
            chunkCount = chunkList.Count,
            content = fullText,
        };

        var jsonText = JsonSerializer.Serialize(
            response,
            new JsonSerializerOptions { WriteIndented = true }
        );

        return new TextResourceContents
        {
            Uri =
                requestContext.Params?.Uri
                ?? $"resource://knowledge/{collectionId}/document/{documentId}",
            MimeType = "application/json",
            Text = jsonText,
        };
    }

    /// <summary>
    /// Retrieves analytics and usage statistics for a specific knowledge collection.
    /// URI: resource://knowledge/{collectionId}/stats
    /// </summary>
    [
        McpServerResource(
            UriTemplate = "resource://knowledge/{collectionId}/stats",
            Name = "Collection Statistics",
            MimeType = "application/json"
        ),
        Description("Analytics and usage statistics for a knowledge collection")
    ]
    public static async Task<ResourceContents> GetCollectionStats(
        RequestContext<ReadResourceRequestParams> requestContext,
        string collectionId,
        IKnowledgeRepository repository,
        IUsageTrackingService usageTracking,
        ILogger<IKnowledgeRepository> logger,
        CancellationToken cancellationToken = default
    )
    {
        logger.LogInformation("MCP Resource: GetCollectionStats for {CollectionId}", collectionId);

        // Validate collection exists
        var exists = await repository.ExistsAsync(collectionId, cancellationToken);
        if (!exists)
            throw new KeyNotFoundException($"Collection not found: {collectionId}");

        // Get collection metadata
        var collections = await repository.GetAllAsync(cancellationToken);
        var collection = collections.FirstOrDefault(c => c.Id == collectionId);

        // Get usage statistics
        var allKnowledgeStats = await usageTracking.GetKnowledgeUsageStatsAsync(cancellationToken);
        var usageStats = allKnowledgeStats.FirstOrDefault(k => k.KnowledgeId == collectionId);

        var response = new
        {
            collectionId = collection?.Id,
            name = collection?.Name,
            documentCount = collection?.DocumentCount ?? 0,
            chunkCount = usageStats?.ChunkCount ?? 0,
            totalQueries = usageStats?.QueryCount ?? 0,
            conversationCount = usageStats?.ConversationCount ?? 0,
            lastQueried = usageStats?.LastQueried,
            createdAt = usageStats?.CreatedAt,
            vectorStore = usageStats?.VectorStore ?? "Qdrant",
            totalFileSize = usageStats?.TotalFileSize ?? 0,
        };

        var jsonText = JsonSerializer.Serialize(
            response,
            new JsonSerializerOptions { WriteIndented = true }
        );

        return new TextResourceContents
        {
            Uri = requestContext.Params?.Uri ?? $"resource://knowledge/{collectionId}/stats",
            MimeType = "application/json",
            Text = jsonText,
        };
    }

    /// <summary>
    /// Retrieves overall system health status including all components.
    /// URI: resource://system/health
    /// </summary>
    [
        McpServerResource(
            UriTemplate = "resource://system/health",
            Name = "System Health",
            MimeType = "application/json"
        ),
        Description("System health status for vector stores, databases, and AI providers")
    ]
    public static async Task<ResourceContents> GetSystemHealth(
        RequestContext<ReadResourceRequestParams> requestContext,
        ISystemHealthService systemHealth,
        ILogger<ISystemHealthService> logger,
        CancellationToken cancellationToken = default
    )
    {
        logger.LogInformation("MCP Resource: GetSystemHealth");

        var healthStatus = await systemHealth.GetSystemHealthAsync(cancellationToken);

        var jsonText = JsonSerializer.Serialize(
            healthStatus,
            new JsonSerializerOptions { WriteIndented = true }
        );

        return new TextResourceContents
        {
            Uri = requestContext.Params?.Uri ?? "resource://system/health",
            MimeType = "application/json",
            Text = jsonText,
        };
    }

    /// <summary>
    /// Retrieves inventory of all available AI models with usage statistics.
    /// URI: resource://system/models
    /// </summary>
    [
        McpServerResource(
            UriTemplate = "resource://system/models",
            Name = "AI Models Inventory",
            MimeType = "application/json"
        ),
        Description("Inventory of AI models with usage stats (Ollama, OpenAI, Anthropic, Google)")
    ]
    public static async Task<ResourceContents> GetModels(
        RequestContext<ReadResourceRequestParams> requestContext,
        IUsageTrackingService usageTracking,
        ILogger<IUsageTrackingService> logger,
        CancellationToken cancellationToken = default
    )
    {
        logger.LogInformation("MCP Resource: GetModels");

        var modelStats = await usageTracking.GetModelUsageStatsAsync(cancellationToken);
        var modelStatsList = modelStats.ToList();

        var models = modelStatsList
            .Select(m => new
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
                failedRequests = m.FailedRequests,
            })
            .ToList();

        var response = new
        {
            totalModels = models.Count,
            providers = modelStatsList.Select(m => m.Provider.ToString()).Distinct().ToList(),
            models,
        };

        var jsonText = JsonSerializer.Serialize(
            response,
            new JsonSerializerOptions { WriteIndented = true }
        );

        return new TextResourceContents
        {
            Uri = requestContext.Params?.Uri ?? "resource://system/models",
            MimeType = "application/json",
            Text = jsonText,
        };
    }
}
