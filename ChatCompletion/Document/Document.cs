public class Document
{
    
    public string? Title { get; set; }                   // Optional: inferred from first heading or filename
    public required string Source { get; set; }                  // e.g. "licence_dashboard" or file path
    public List<string> Tags { get; set; }              // Optional: ["licence", "user", "report"]
    
    public List<IDocumentElement> Elements { get; }     // The body content, in order

    public Document()
    {
        Elements = new List<IDocumentElement>();
        Tags = new List<string>();
    }
}
