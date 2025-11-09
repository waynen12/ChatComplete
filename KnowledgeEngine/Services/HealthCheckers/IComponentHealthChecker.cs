using KnowledgeEngine.Agents.Models;

namespace KnowledgeEngine.Services.HealthCheckers;

/// <summary>
/// Base interface for component-specific health checkers
/// </summary>
public interface IComponentHealthChecker
{
    /// <summary>
    /// Name of the component this checker monitors
    /// </summary>
    string ComponentName { get; }

    /// <summary>
    /// Priority order for health checks (lower numbers = higher priority)
    /// </summary>
    int Priority { get; }

    /// <summary>
    /// Whether this component is critical to system operation
    /// </summary>
    bool IsCriticalComponent { get; }

    /// <summary>
    /// Performs a health check on the component
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Health status of the component</returns>
    Task<ComponentHealth> CheckHealthAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs a quick health check with minimal testing
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Basic health status</returns>
    Task<ComponentHealth> QuickHealthCheckAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets component-specific metrics and additional information
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Dictionary of component metrics</returns>
    Task<Dictionary<string, object>> GetComponentMetricsAsync(CancellationToken cancellationToken = default);
}