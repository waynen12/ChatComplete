namespace Knowledge.Mcp.Resources.Models;

/// <summary>
/// Result of a resources/list request.
/// Contains catalog of all available resources.
/// </summary>
public class ResourceListResult
{
    /// <summary>
    /// List of all resources available from this server.
    /// </summary>
    public List<ResourceMetadata> Resources { get; set; } = new();

    /// <summary>
    /// Optional cursor for pagination (future use).
    /// Null if all resources are included in this response.
    /// </summary>
    public string? NextCursor { get; set; }
}
