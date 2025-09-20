namespace KnowledgeEngine.Agents.Models;

/// <summary>
/// Represents the health status of a specific system component (provider, database, service, etc.)
/// </summary>
public class ComponentHealth
{
    /// <summary>
    /// Name of the component being monitored (e.g., "OpenAI", "Qdrant", "SQLite")
    /// </summary>
    public string ComponentName { get; set; } = string.Empty;

    /// <summary>
    /// Current health status: Healthy, Warning, Critical, Offline, Unknown
    /// </summary>
    public string Status { get; set; } = "Unknown";

    /// <summary>
    /// Human-readable status message with additional context
    /// </summary>
    public string StatusMessage { get; set; } = string.Empty;

    /// <summary>
    /// When this component was last checked
    /// </summary>
    public DateTime LastChecked { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Response time for the last health check
    /// </summary>
    public TimeSpan ResponseTime { get; set; }

    /// <summary>
    /// Component-specific metrics and additional data
    /// </summary>
    public Dictionary<string, object> Metrics { get; set; } = new();

    /// <summary>
    /// Version information for the component (if available)
    /// </summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// Whether the component is currently reachable/connected
    /// </summary>
    public bool IsConnected { get; set; }

    /// <summary>
    /// Number of recent errors detected for this component
    /// </summary>
    public int ErrorCount { get; set; }

    /// <summary>
    /// Timestamp of the last successful operation with this component
    /// </summary>
    public DateTime LastSuccess { get; set; }

    /// <summary>
    /// Formatted response time for display
    /// </summary>
    public string FormattedResponseTime => 
        ResponseTime.TotalMilliseconds < 1000 
            ? $"{ResponseTime.TotalMilliseconds:F0}ms"
            : $"{ResponseTime.TotalSeconds:F1}s";

    /// <summary>
    /// Time since last successful operation
    /// </summary>
    public string TimeSinceLastSuccess
    {
        get
        {
            if (LastSuccess == default) return "Never";
            
            var elapsed = DateTime.UtcNow - LastSuccess;
            if (elapsed.TotalMinutes < 1) return "Just now";
            if (elapsed.TotalHours < 1) return $"{elapsed.Minutes}m ago";
            if (elapsed.TotalDays < 1) return $"{elapsed.Hours}h ago";
            return $"{elapsed.Days}d ago";
        }
    }

    /// <summary>
    /// Determines if this component is considered healthy
    /// </summary>
    public bool IsHealthy => Status.Equals("Healthy", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Determines if this component has warnings
    /// </summary>
    public bool HasWarnings => Status.Equals("Warning", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Determines if this component is in critical state
    /// </summary>
    public bool IsCritical => Status.Equals("Critical", StringComparison.OrdinalIgnoreCase);
}