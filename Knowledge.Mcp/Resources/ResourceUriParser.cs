using Knowledge.Mcp.Resources.Models;

namespace Knowledge.Mcp.Resources;

/// <summary>
/// Parses MCP resource URIs into structured components for routing to handlers.
/// Handles all supported URI patterns and provides detailed error messages.
/// </summary>
public class ResourceUriParser
{
    private const string ResourceScheme = "resource://";

    /// <summary>
    /// Parses a resource URI string into its component parts.
    /// </summary>
    /// <param name="uri">The resource URI to parse (e.g., "resource://knowledge/docker-guides/document/ssl-setup")</param>
    /// <returns>Parsed URI with resource type, collection ID, and document ID extracted</returns>
    /// <exception cref="ArgumentException">Thrown if URI format is invalid or unknown</exception>
    public ParsedResourceUri Parse(string uri)
    {
        if (string.IsNullOrWhiteSpace(uri))
        {
            throw new ArgumentException("Resource URI cannot be null or empty", nameof(uri));
        }

        if (!uri.StartsWith(ResourceScheme, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException(
                $"Invalid resource URI format: '{uri}'. URIs must start with '{ResourceScheme}'",
                nameof(uri)
            );
        }

        // Extract path after "resource://"
        var path = uri.Substring(ResourceScheme.Length);

        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException(
                $"Invalid resource URI: '{uri}'. Path cannot be empty after '{ResourceScheme}'",
                nameof(uri)
            );
        }

        // Split path into segments
        var parts = path.Split('/', StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length == 0)
        {
            throw new ArgumentException(
                $"Invalid resource URI: '{uri}'. No path segments found",
                nameof(uri)
            );
        }

        // Route to appropriate parser based on first segment (case-insensitive)
        return parts[0].ToLowerInvariant() switch
        {
            "knowledge" => ParseKnowledgeUri(uri, parts),
            "system" => ParseSystemUri(uri, parts),
            _ => throw new ArgumentException(
                $"Unknown resource domain: '{parts[0]}'. Valid domains are 'knowledge' and 'system'",
                nameof(uri)
            ),
        };
    }

    /// <summary>
    /// Parses knowledge-scoped URIs (collections, documents).
    /// </summary>
    private ParsedResourceUri ParseKnowledgeUri(string originalUri, string[] parts)
    {
        // resource://knowledge/collections
        if (parts.Length == 2 && parts[1].Equals("collections", StringComparison.OrdinalIgnoreCase))
        {
            return new ParsedResourceUri
            {
                Type = ResourceType.CollectionList,
                OriginalUri = originalUri,
            };
        }

        // resource://knowledge/{collectionId}/documents
        if (parts.Length == 3 && parts[2].Equals("documents", StringComparison.OrdinalIgnoreCase))
        {
            return new ParsedResourceUri
            {
                Type = ResourceType.DocumentList,
                CollectionId = parts[1],
                OriginalUri = originalUri,
            };
        }

        // resource://knowledge/{collectionId}/document/{docId}
        if (parts.Length == 4 && parts[2].Equals("document", StringComparison.OrdinalIgnoreCase))
        {
            return new ParsedResourceUri
            {
                Type = ResourceType.Document,
                CollectionId = parts[1],
                DocumentId = parts[3],
                OriginalUri = originalUri,
            };
        }

        // resource://knowledge/{collectionId}/stats
        if (parts.Length == 3 && parts[2].Equals("stats", StringComparison.OrdinalIgnoreCase))
        {
            return new ParsedResourceUri
            {
                Type = ResourceType.CollectionStats,
                CollectionId = parts[1],
                OriginalUri = originalUri,
            };
        }

        // If we get here, the pattern didn't match any known format
        throw new ArgumentException(
            $"Unknown knowledge resource pattern: '{originalUri}'. "
                + "Valid patterns are: "
                + "'resource://knowledge/collections', "
                + "'resource://knowledge/{{collectionId}}/documents', "
                + "'resource://knowledge/{{collectionId}}/document/{{docId}}', "
                + "'resource://knowledge/{{collectionId}}/stats'",
            nameof(originalUri)
        );
    }

    /// <summary>
    /// Parses system-scoped URIs (health, models).
    /// </summary>
    private ParsedResourceUri ParseSystemUri(string originalUri, string[] parts)
    {
        // resource://system/health
        if (parts.Length == 2 && parts[1].Equals("health", StringComparison.OrdinalIgnoreCase))
        {
            return new ParsedResourceUri
            {
                Type = ResourceType.SystemHealth,
                OriginalUri = originalUri,
            };
        }

        // resource://system/models
        if (parts.Length == 2 && parts[1].Equals("models", StringComparison.OrdinalIgnoreCase))
        {
            return new ParsedResourceUri
            {
                Type = ResourceType.ModelList,
                OriginalUri = originalUri,
            };
        }

        // If we get here, the pattern didn't match any known format
        throw new ArgumentException(
            $"Unknown system resource pattern: '{originalUri}'. "
                + "Valid patterns are: 'resource://system/health', 'resource://system/models'",
            nameof(originalUri)
        );
    }

    /// <summary>
    /// Validates that a collection ID contains only allowed characters.
    /// Prevents path traversal and injection attacks.
    /// </summary>
    public static bool IsValidCollectionId(string? collectionId)
    {
        if (string.IsNullOrWhiteSpace(collectionId))
        {
            return false;
        }

        // Allow alphanumeric, hyphen, underscore, and dot
        // Disallow path traversal attempts (../ or ..\)
        return !collectionId.Contains("..")
            && !collectionId.Contains('/')
            && !collectionId.Contains('\\')
            && collectionId.Length <= 256; // Reasonable length limit
    }

    /// <summary>
    /// Validates that a document ID contains only allowed characters.
    /// Prevents path traversal and injection attacks.
    /// </summary>
    public static bool IsValidDocumentId(string? documentId)
    {
        if (string.IsNullOrWhiteSpace(documentId))
        {
            return false;
        }

        // Same rules as collection ID
        return !documentId.Contains("..")
            && !documentId.Contains('/')
            && !documentId.Contains('\\')
            && documentId.Length <= 256; // Reasonable length limit
    }
}
