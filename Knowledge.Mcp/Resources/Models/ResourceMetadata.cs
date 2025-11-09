namespace Knowledge.Mcp.Resources.Models;

/// <summary>
/// Metadata describing an available resource.
/// Returned by resources/list to advertise what clients can read.
/// </summary>
public class ResourceMetadata
{
    /// <summary>
    /// Unique URI identifying this resource.
    /// Example: "resource://knowledge/docker-guides/document/ssl-setup"
    /// </summary>
    public required string Uri { get; set; }

    /// <summary>
    /// Human-readable name of the resource.
    /// Example: "Docker SSL Setup Guide"
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Description explaining what this resource contains.
    /// Example: "Complete documentation for setting up SSL with Docker"
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// MIME type of the resource content.
    /// Examples: "text/markdown", "application/json", "text/plain"
    /// </summary>
    public required string MimeType { get; set; }

    /// <summary>
    /// Optional annotations providing additional hints to clients.
    /// Can include priority, audience (user/developer), tags, etc.
    /// </summary>
    public Dictionary<string, object>? Annotations { get; set; }
}
