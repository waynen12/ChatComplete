namespace Knowledge.Contracts;

/// <summary>
/// Data transfer object representing a summary of knowledge information.
/// </summary>
public class KnowledgeSummaryDto
{

    /// <summary>
    /// Gets or sets the unique identifier for the knowledge summary.
    /// </summary>
    public string Id { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the name of the knowledge summary.
    /// </summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the count of documents associated with this knowledge summary.
    /// </summary>
    public int DocumentCount { get; set; }
}
