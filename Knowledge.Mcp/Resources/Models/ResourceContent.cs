namespace Knowledge.Mcp.Resources.Models;

/// <summary>
/// Content of a resource being read.
/// Returned by resources/read when client requests specific resource.
/// </summary>
public class ResourceContent
{
    /// <summary>
    /// URI of the resource being returned.
    /// Matches the URI from the client's request.
    /// </summary>
    public required string Uri { get; set; }

    /// <summary>
    /// MIME type of the content.
    /// Helps client understand how to parse/display the data.
    /// </summary>
    public required string MimeType { get; set; }

    /// <summary>
    /// Actual content of the resource as text.
    /// For JSON: serialized JSON string
    /// For markdown: raw markdown text
    /// For plain text: document text
    /// </summary>
    public required string Text { get; set; }

    /// <summary>
    /// Optional binary content (for future use).
    /// Currently not used - all content returned as text.
    /// </summary>
    public byte[]? Blob { get; set; }
}
