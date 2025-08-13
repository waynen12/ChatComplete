using System.ComponentModel.DataAnnotations;

namespace Knowledge.Contracts;

/// <summary>
/// Represents the overall health status of the application.
/// </summary>
public class HealthCheckDto
{
    /// <summary>
    /// Overall health status (healthy, degraded, unhealthy).
    /// </summary>
    [Required]
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp when the health check was performed.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Current environment (Development, Staging, Production).
    /// </summary>
    public string? Environment { get; set; }

    /// <summary>
    /// Whether the application is running in a container.
    /// </summary>
    public bool Container { get; set; }

    /// <summary>
    /// Individual health check results for various system components.
    /// </summary>
    public Dictionary<string, HealthCheckComponent> Checks { get; set; } = new();

    /// <summary>
    /// Error message if the overall health check failed.
    /// </summary>
    public string? Error { get; set; }
}

/// <summary>
/// Represents the health status of a specific system component.
/// </summary>
public class HealthCheckComponent
{
    /// <summary>
    /// Component health status (healthy, warning, unhealthy, error, unknown).
    /// </summary>
    [Required]
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Number of collections (for VectorStore component).
    /// </summary>
    public int? Collections { get; set; }

    /// <summary>
    /// Available disk space in GB (for DiskSpace component).
    /// </summary>
    public double? AvailableGB { get; set; }

    /// <summary>
    /// Total disk space in GB (for DiskSpace component).
    /// </summary>
    public double? TotalGB { get; set; }

    /// <summary>
    /// Working set memory in MB (for Memory component).
    /// </summary>
    public double? WorkingSetMB { get; set; }

    /// <summary>
    /// Error message if the component check failed.
    /// </summary>
    public string? Error { get; set; }

    /// <summary>
    /// General message for additional information.
    /// </summary>
    public string? Message { get; set; }
}