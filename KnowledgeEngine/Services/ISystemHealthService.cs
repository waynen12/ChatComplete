using KnowledgeEngine.Agents.Models;

namespace KnowledgeEngine.Services;

/// <summary>
/// Service for monitoring and reporting system health across all components
/// </summary>
public interface ISystemHealthService
{
    /// <summary>
    /// Gets comprehensive system health status including all components and metrics
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Complete system health status</returns>
    Task<SystemHealthStatus> GetSystemHealthAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks the health of a specific system component
    /// </summary>
    /// <param name="componentName">Name of the component to check (e.g., "OpenAI", "Qdrant")</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Health status of the specified component</returns>
    Task<ComponentHealth> CheckComponentHealthAsync(string componentName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs health checks on all registered system components
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of health statuses for all components</returns>
    Task<List<ComponentHealth>> CheckAllComponentsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gathers system performance and resource metrics
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Current system metrics</returns>
    Task<SystemMetrics> GetSystemMetricsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates intelligent recommendations based on current system health
    /// </summary>
    /// <param name="healthStatus">Current system health status</param>
    /// <returns>List of actionable recommendations</returns>
    Task<List<string>> GetHealthRecommendationsAsync(SystemHealthStatus healthStatus);

    /// <summary>
    /// Gets the list of available components that can be health checked
    /// </summary>
    /// <returns>List of component names that can be individually checked</returns>
    Task<List<string>> GetAvailableComponentsAsync();

    /// <summary>
    /// Performs a quick health check focusing on critical components only
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Simplified health status for quick overview</returns>
    Task<SystemHealthStatus> GetQuickHealthCheckAsync(CancellationToken cancellationToken = default);
}