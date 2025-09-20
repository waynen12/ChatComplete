namespace KnowledgeEngine.Agents.Models;

/// <summary>
/// Comprehensive system performance and resource metrics
/// </summary>
public class SystemMetrics
{
    /// <summary>
    /// Total tokens consumed across all providers this month
    /// </summary>
    public long TotalTokensUsed { get; set; }

    /// <summary>
    /// Estimated monthly cost based on current usage patterns
    /// </summary>
    public decimal EstimatedMonthlyCost { get; set; }

    /// <summary>
    /// Total number of conversations processed
    /// </summary>
    public int TotalConversations { get; set; }

    /// <summary>
    /// Number of active knowledge bases in the system
    /// </summary>
    public int ActiveKnowledgeBases { get; set; }

    /// <summary>
    /// Total size of the SQLite database in bytes
    /// </summary>
    public long DatabaseSizeBytes { get; set; }

    /// <summary>
    /// Number of collections in the vector store
    /// </summary>
    public long VectorStoreCollections { get; set; }

    /// <summary>
    /// Average response time across all operations (in milliseconds)
    /// </summary>
    public double AverageResponseTime { get; set; }

    /// <summary>
    /// Success rate as a percentage (0-100)
    /// </summary>
    public double SuccessRate { get; set; }

    /// <summary>
    /// Number of errors in the last 24 hours
    /// </summary>
    public int ErrorsLast24Hours { get; set; }

    /// <summary>
    /// When the system was started
    /// </summary>
    public DateTime SystemStartTime { get; set; }

    /// <summary>
    /// Peak memory usage in bytes
    /// </summary>
    public long PeakMemoryUsage { get; set; }

    /// <summary>
    /// Current memory usage in bytes
    /// </summary>
    public long CurrentMemoryUsage { get; set; }

    /// <summary>
    /// Number of concurrent connections
    /// </summary>
    public int ActiveConnections { get; set; }

    /// <summary>
    /// Formatted database size for human readability
    /// </summary>
    public string FormattedDatabaseSize => FormatBytes(DatabaseSizeBytes);

    /// <summary>
    /// Formatted peak memory usage
    /// </summary>
    public string FormattedPeakMemory => FormatBytes(PeakMemoryUsage);

    /// <summary>
    /// Formatted current memory usage
    /// </summary>
    public string FormattedCurrentMemory => FormatBytes(CurrentMemoryUsage);

    /// <summary>
    /// System uptime
    /// </summary>
    public TimeSpan Uptime => DateTime.UtcNow - SystemStartTime;

    /// <summary>
    /// Formatted uptime for display
    /// </summary>
    public string FormattedUptime
    {
        get
        {
            var uptime = Uptime;
            if (uptime.TotalDays >= 1)
                return $"{uptime.Days}d {uptime.Hours}h {uptime.Minutes}m";
            if (uptime.TotalHours >= 1)
                return $"{uptime.Hours}h {uptime.Minutes}m";
            return $"{uptime.Minutes}m {uptime.Seconds}s";
        }
    }

    /// <summary>
    /// Formatted success rate with percentage
    /// </summary>
    public string FormattedSuccessRate => $"{SuccessRate:F1}%";

    /// <summary>
    /// Formatted average response time
    /// </summary>
    public string FormattedAverageResponseTime =>
        AverageResponseTime < 1000
            ? $"{AverageResponseTime:F0}ms"
            : $"{AverageResponseTime / 1000:F1}s";

    /// <summary>
    /// Formatted estimated monthly cost
    /// </summary>
    public string FormattedEstimatedCost => $"${EstimatedMonthlyCost:F2}";

    /// <summary>
    /// Formatted token usage with units
    /// </summary>
    public string FormattedTokenUsage
    {
        get
        {
            if (TotalTokensUsed >= 1_000_000)
                return $"{TotalTokensUsed / 1_000_000.0:F1}M tokens";
            if (TotalTokensUsed >= 1_000)
                return $"{TotalTokensUsed / 1_000.0:F1}K tokens";
            return $"{TotalTokensUsed} tokens";
        }
    }

    /// <summary>
    /// Formats bytes into human-readable size units
    /// </summary>
    private static string FormatBytes(long bytes)
    {
        if (bytes == 0) return "0 bytes";

        string[] sizes = { "bytes", "KB", "MB", "GB", "TB" };
        int order = 0;
        double size = bytes;

        while (size >= 1024 && order < sizes.Length - 1)
        {
            order++;
            size /= 1024;
        }

        return $"{size:F1} {sizes[order]}";
    }

    /// <summary>
    /// Determines if system performance is within acceptable limits
    /// </summary>
    public bool IsPerformanceHealthy =>
        SuccessRate >= 95.0 &&
        AverageResponseTime <= 2000 &&
        ErrorsLast24Hours <= 10;

    /// <summary>
    /// Determines if resource usage is concerning
    /// </summary>
    public bool HasResourceConcerns =>
        DatabaseSizeBytes > 1_000_000_000 || // > 1GB
        ErrorsLast24Hours > 50 ||
        SuccessRate < 90.0;
}