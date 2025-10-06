namespace Knowledge.Mcp.Resources.Models;

/// <summary>
/// Result of a resources/read request.
/// Contains the actual content of requested resource(s).
/// </summary>
public class ResourceReadResult
{
    /// <summary>
    /// Content of the requested resource.
    /// Typically contains a single item, but array supports future multi-resource reads.
    /// </summary>
    public ResourceContent[] Contents { get; set; } = Array.Empty<ResourceContent>();
}
