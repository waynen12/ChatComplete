namespace KnowledgeEngine.Agents.Models;

/// <summary>
/// Data model representing analytics and summary information for a knowledge base
/// </summary>
public class KnowledgeBaseSummary
{
    /// <summary>
    /// Unique identifier for the knowledge base
    /// </summary>
    public string KnowledgeId { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable display name for the knowledge base
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Total number of documents/files in the knowledge base
    /// </summary>
    public int DocumentCount { get; set; }

    /// <summary>
    /// Total number of vector chunks in the knowledge base
    /// </summary>
    public int ChunkCount { get; set; }

    /// <summary>
    /// Total size of all documents in bytes
    /// </summary>
    public long SizeBytes { get; set; }

    /// <summary>
    /// When the knowledge base was last updated/modified
    /// </summary>
    public DateTime LastUpdated { get; set; }

    /// <summary>
    /// Number of queries made to this knowledge base in the last 30 days
    /// </summary>
    public int MonthlyQueryCount { get; set; }

    /// <summary>
    /// Activity level classification: "High", "Medium", "Low", "None"
    /// </summary>
    public string ActivityLevel { get; set; } = "None";

    /// <summary>
    /// Human-readable size string (e.g., "12.4 MB", "3.2 KB")
    /// </summary>
    public string FormattedSize
    {
        get
        {
            if (SizeBytes >= 1024 * 1024 * 1024)
                return $"{SizeBytes / (1024.0 * 1024 * 1024):F1} GB";
            if (SizeBytes >= 1024 * 1024)
                return $"{SizeBytes / (1024.0 * 1024):F1} MB";
            if (SizeBytes >= 1024)
                return $"{SizeBytes / 1024.0:F1} KB";
            return $"{SizeBytes} bytes";
        }
    }

    /// <summary>
    /// Human-readable last updated string (e.g., "3 days ago", "2 weeks ago")
    /// </summary>
    public string FormattedLastUpdated
    {
        get
        {
            var timeSpan = DateTime.UtcNow - LastUpdated;
            
            if (timeSpan.TotalDays >= 365)
                return $"{(int)(timeSpan.TotalDays / 365)} year{((int)(timeSpan.TotalDays / 365) == 1 ? "" : "s")} ago";
            if (timeSpan.TotalDays >= 30)
                return $"{(int)(timeSpan.TotalDays / 30)} month{((int)(timeSpan.TotalDays / 30) == 1 ? "" : "s")} ago";
            if (timeSpan.TotalDays >= 7)
                return $"{(int)(timeSpan.TotalDays / 7)} week{((int)(timeSpan.TotalDays / 7) == 1 ? "" : "s")} ago";
            if (timeSpan.TotalDays >= 1)
                return $"{(int)timeSpan.TotalDays} day{((int)timeSpan.TotalDays == 1 ? "" : "s")} ago";
            if (timeSpan.TotalHours >= 1)
                return $"{(int)timeSpan.TotalHours} hour{((int)timeSpan.TotalHours == 1 ? "" : "s")} ago";
            if (timeSpan.TotalMinutes >= 1)
                return $"{(int)timeSpan.TotalMinutes} minute{((int)timeSpan.TotalMinutes == 1 ? "" : "s")} ago";
            
            return "Just now";
        }
    }
}