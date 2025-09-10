using Knowledge.Analytics.Models;

namespace Knowledge.Analytics.Services;

/// <summary>
/// Service for Ollama-specific analytics and metrics aggregation
/// </summary>
public interface IOllamaAnalyticsService
{
    /// <summary>
    /// Gets comprehensive Ollama usage and resource metrics
    /// </summary>
    Task<OllamaUsageInfo> GetUsageInfoAsync(int days = 30, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets model inventory and resource usage statistics
    /// </summary>
    Task<OllamaModelInventory> GetModelInventoryAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets download activity and success statistics
    /// </summary>
    Task<OllamaDownloadStats> GetDownloadStatsAsync(int days = 30, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets performance metrics by model
    /// </summary>
    Task<IEnumerable<OllamaModelPerformance>> GetModelPerformanceAsync(int days = 30, CancellationToken cancellationToken = default);
}