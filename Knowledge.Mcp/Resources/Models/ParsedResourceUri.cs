namespace Knowledge.Mcp.Resources.Models;

/// <summary>
/// Result of parsing a resource URI into its component parts.
/// Used to route resource requests to appropriate handlers.
/// </summary>
public class ParsedResourceUri
{
    /// <summary>
    /// The type of resource being requested.
    /// Determines which handler method to invoke.
    /// </summary>
    public ResourceType Type { get; set; }

    /// <summary>
    /// Collection identifier (for knowledge-scoped resources).
    /// Null for system-scoped resources (health, models).
    /// Example: "docker-guides" from "resource://knowledge/docker-guides/documents"
    /// </summary>
    public string? CollectionId { get; set; }

    /// <summary>
    /// Document identifier (for document-scoped resources).
    /// Null for collection or system-scoped resources.
    /// Example: "ssl-setup" from "resource://knowledge/docker-guides/document/ssl-setup"
    /// </summary>
    public string? DocumentId { get; set; }

    /// <summary>
    /// Original URI that was parsed.
    /// Useful for logging and error messages.
    /// </summary>
    public string OriginalUri { get; set; } = string.Empty;
}
