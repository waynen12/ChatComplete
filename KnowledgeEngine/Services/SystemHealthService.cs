using System.Diagnostics;
using Knowledge.Analytics.Services;
using KnowledgeEngine.Agents.Models;
using KnowledgeEngine.Services.HealthCheckers;
using Microsoft.Extensions.Logging;

namespace KnowledgeEngine.Services;

/// <summary>
/// Main service for comprehensive system health monitoring and reporting
/// </summary>
public class SystemHealthService : ISystemHealthService
{
    private readonly IEnumerable<IComponentHealthChecker> _healthCheckers;
    private readonly IUsageTrackingService _usageTrackingService;
    private readonly ILogger<SystemHealthService> _logger;

    public SystemHealthService(
        IEnumerable<IComponentHealthChecker> healthCheckers,
        IUsageTrackingService usageTrackingService,
        ILogger<SystemHealthService> logger
    )
    {
        _healthCheckers = healthCheckers;
        _usageTrackingService = usageTrackingService;
        _logger = logger;
    }

    public async Task<SystemHealthStatus> GetSystemHealthAsync(
        CancellationToken cancellationToken = default
    )
    {
        _logger.LogInformation("Starting comprehensive system health check");
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var systemHealth = new SystemHealthStatus { LastChecked = DateTime.UtcNow };

            // Check all components in parallel
            var componentTasks = _healthCheckers
                .OrderBy(hc => hc.Priority)
                .Select(hc => CheckComponentSafelyAsync(hc, cancellationToken))
                .ToArray();

            var componentResults = await Task.WhenAll(componentTasks);
            systemHealth.Components.AddRange(componentResults.Where(c => c != null)!);

            // Get system metrics
            systemHealth.Metrics = await GetSystemMetricsAsync(cancellationToken);

            // Update overall status based on component health
            systemHealth.UpdateOverallStatus();

            // Generate alerts and recommendations
            await GenerateAlertsAndRecommendationsAsync(systemHealth);

            stopwatch.Stop();
            _logger.LogInformation(
                "System health check completed in {Duration}ms. Status: {Status}",
                stopwatch.ElapsedMilliseconds,
                systemHealth.OverallStatus
            );

            return systemHealth;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during system health check");
            return CreateErrorSystemHealth(ex.Message);
        }
    }

    public async Task<ComponentHealth> CheckComponentHealthAsync(
        string componentName,
        CancellationToken cancellationToken = default
    )
    {
        _logger.LogInformation("Checking health for component: {ComponentName}", componentName);

        var healthChecker = _healthCheckers.FirstOrDefault(hc =>
            string.Equals(hc.ComponentName, componentName, StringComparison.OrdinalIgnoreCase)
        );

        if (healthChecker == null)
        {
            _logger.LogWarning(
                "No health checker found for component: {ComponentName}",
                componentName
            );
            return new ComponentHealth
            {
                ComponentName = componentName,
                Status = "Unknown",
                StatusMessage = "No health checker available for this component",
                LastChecked = DateTime.UtcNow,
                IsConnected = false,
            };
        }

        return await CheckComponentSafelyAsync(healthChecker, cancellationToken)
            ?? CreateErrorComponentHealth(componentName, "Health check failed");
    }

    public async Task<List<ComponentHealth>> CheckAllComponentsAsync(
        CancellationToken cancellationToken = default
    )
    {
        _logger.LogInformation(
            "Checking health for all {Count} components",
            _healthCheckers.Count()
        );

        var componentTasks = _healthCheckers
            .OrderBy(hc => hc.Priority)
            .Select(hc => CheckComponentSafelyAsync(hc, cancellationToken))
            .ToArray();

        var results = await Task.WhenAll(componentTasks);
        return results.Where(c => c != null).ToList()!;
    }

    public async Task<SystemMetrics> GetSystemMetricsAsync(
        CancellationToken cancellationToken = default
    )
    {
        _logger.LogDebug("Gathering system metrics");

        try
        {
            var metrics = new SystemMetrics
            {
                SystemStartTime = DateTime.UtcNow, // For now, use current time as system start
            };

            // Get usage statistics from the last 30 days
            var usageHistory = await _usageTrackingService.GetUsageHistoryAsync(
                30,
                cancellationToken
            );
            var usageList = usageHistory.ToList();

            if (usageList.Any())
            {
                var last24Hours = usageList
                    .Where(u => u.Timestamp >= DateTime.UtcNow.AddDays(-1))
                    .ToList();
                var successful = usageList.Where(u => u.WasSuccessful).ToList();

                metrics.TotalConversations = usageList.Count;
                metrics.SuccessRate =
                    usageList.Count > 0 ? (double)successful.Count / usageList.Count * 100 : 0;
                metrics.ErrorsLast24Hours = last24Hours.Count(u => !u.WasSuccessful);
                metrics.TotalTokensUsed = usageList.Sum(u => u.InputTokens + u.OutputTokens);

                if (successful.Any())
                {
                    metrics.AverageResponseTime = successful.Average(u =>
                        u.ResponseTime.TotalMilliseconds
                    );
                }
            }

            // Get current database size estimate
            metrics.DatabaseSizeBytes = await EstimateDatabaseSizeAsync(cancellationToken);

            _logger.LogDebug(
                "System metrics gathered: {Conversations} total conversations, {SuccessRate}% success rate",
                metrics.TotalConversations,
                metrics.SuccessRate
            );

            return metrics;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error gathering system metrics");
            return new SystemMetrics
            {
                SystemStartTime = DateTime.UtcNow,
                ErrorsLast24Hours = 1, // Count this error
            };
        }
    }

    public Task<List<string>> GetHealthRecommendationsAsync(SystemHealthStatus healthStatus)
    {
        var recommendations = new List<string>();

        try
        {
            // Database-specific recommendations
            var sqliteHealth = healthStatus.GetComponentHealth("SQLite");
            if (sqliteHealth != null)
            {
                if (!sqliteHealth.IsConnected)
                {
                    recommendations.Add(
                        "Database is not accessible. Check file permissions and disk space."
                    );
                }
                else if (sqliteHealth.ResponseTime.TotalMilliseconds > 1000)
                {
                    recommendations.Add(
                        "Database response time is slow. Consider optimizing queries or checking disk I/O."
                    );
                }

                if (
                    sqliteHealth.Metrics.TryGetValue("DatabaseSizeBytes", out var sizeObj)
                    && sizeObj is long size
                    && size > 1_000_000_000
                ) // 1GB
                {
                    recommendations.Add(
                        "Database size is large (>1GB). Consider archiving old data or implementing cleanup policies."
                    );
                }
            }

            // Vector store recommendations
            var qdrantHealth = healthStatus.GetComponentHealth("Qdrant");
            if (qdrantHealth != null)
            {
                if (!qdrantHealth.IsConnected)
                {
                    recommendations.Add(
                        "Vector store is not accessible. Verify Qdrant service is running and configuration is correct."
                    );
                }
                else if (
                    qdrantHealth.Status == "Warning"
                    && qdrantHealth.Metrics.ContainsKey("CollectionCount")
                )
                {
                    var collectionCount = (int)qdrantHealth.Metrics["CollectionCount"];
                    if (collectionCount == 0)
                    {
                        recommendations.Add(
                            "No vector collections found. Upload some documents to enable semantic search."
                        );
                    }
                }
            }

            // AI service recommendations
            var ollamaHealth = healthStatus.GetComponentHealth("Ollama");
            if (ollamaHealth != null)
            {
                if (!ollamaHealth.IsConnected)
                {
                    recommendations.Add(
                        "Ollama service is not running. Start Ollama to enable local AI models."
                    );
                }
                else if (
                    ollamaHealth.Status == "Warning"
                    && ollamaHealth.Metrics.ContainsKey("ModelCount")
                )
                {
                    var modelCount = (int)ollamaHealth.Metrics["ModelCount"];
                    if (modelCount == 0)
                    {
                        recommendations.Add(
                            "No Ollama models installed. Download models like 'llama3.2:3b' for local AI capabilities."
                        );
                    }
                }
            }

            // System-wide performance recommendations
            if (healthStatus.Metrics.SuccessRate < 95.0)
            {
                recommendations.Add(
                    $"System success rate is {healthStatus.Metrics.SuccessRate:F1}%. Investigate error patterns and improve reliability."
                );
            }

            if (healthStatus.Metrics.AverageResponseTime > 3000)
            {
                recommendations.Add(
                    $"Average response time is {healthStatus.Metrics.AverageResponseTime:F0}ms. Consider optimizing queries or scaling resources."
                );
            }

            if (healthStatus.Metrics.ErrorsLast24Hours > 10)
            {
                recommendations.Add(
                    $"High error count in last 24 hours ({healthStatus.Metrics.ErrorsLast24Hours}). Review error logs and address common issues."
                );
            }

            // Overall system health recommendations
            if (healthStatus.CriticalComponents > 0)
            {
                recommendations.Add(
                    "Critical components detected. Address critical issues immediately to restore full functionality."
                );
            }

            if (healthStatus.SystemHealthPercentage < 75.0)
            {
                recommendations.Add(
                    "System health is below optimal. Review component status and address warnings before they become critical."
                );
            }

            // Add general maintenance recommendations if system is healthy
            if (healthStatus.OverallStatus == "Healthy" && !recommendations.Any())
            {
                recommendations.Add(
                    "System is healthy. Consider regular maintenance like updating models and optimizing knowledge bases."
                );
            }

            _logger.LogDebug("Generated {Count} health recommendations", recommendations.Count);
            return Task.FromResult(recommendations);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating health recommendations");
            return Task.FromResult(
                new List<string>
                {
                    "Unable to generate recommendations due to an error. Check system logs.",
                }
            );
        }
    }

    public Task<List<string>> GetAvailableComponentsAsync()
    {
        try
        {
            var components = _healthCheckers
                .Select(hc => hc.ComponentName)
                .OrderBy(name => name)
                .ToList();

            _logger.LogDebug("Available components: {Components}", string.Join(", ", components));
            return Task.FromResult(components);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting available components");
            return Task.FromResult(new List<string>());
        }
    }

    public async Task<SystemHealthStatus> GetQuickHealthCheckAsync(
        CancellationToken cancellationToken = default
    )
    {
        _logger.LogInformation("Starting quick system health check");
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var systemHealth = new SystemHealthStatus { LastChecked = DateTime.UtcNow };

            // Only check critical components for quick check
            var criticalCheckers = _healthCheckers
                .Where(hc => hc.IsCriticalComponent)
                .OrderBy(hc => hc.Priority)
                .ToList();

            var quickTasks = criticalCheckers
                .Select(hc => PerformQuickCheckSafelyAsync(hc, cancellationToken))
                .ToArray();

            var results = await Task.WhenAll(quickTasks);
            systemHealth.Components.AddRange(results.Where(c => c != null)!);

            // Get simplified metrics (less intensive queries)
            systemHealth.Metrics = await GetQuickSystemMetricsAsync(cancellationToken);

            // Update overall status
            systemHealth.UpdateOverallStatus();

            stopwatch.Stop();
            _logger.LogInformation(
                "Quick health check completed in {Duration}ms. Status: {Status}",
                stopwatch.ElapsedMilliseconds,
                systemHealth.OverallStatus
            );

            return systemHealth;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during quick health check");
            return CreateErrorSystemHealth(ex.Message);
        }
    }

    #region Private Helper Methods

    private async Task<ComponentHealth?> CheckComponentSafelyAsync(
        IComponentHealthChecker healthChecker,
        CancellationToken cancellationToken
    )
    {
        try
        {
            _logger.LogDebug(
                "Checking health for component: {ComponentName}",
                healthChecker.ComponentName
            );
            return await healthChecker.CheckHealthAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error checking health for component: {ComponentName}",
                healthChecker.ComponentName
            );
            return CreateErrorComponentHealth(healthChecker.ComponentName, ex.Message);
        }
    }

    private async Task<ComponentHealth?> PerformQuickCheckSafelyAsync(
        IComponentHealthChecker healthChecker,
        CancellationToken cancellationToken
    )
    {
        try
        {
            _logger.LogDebug(
                "Quick check for component: {ComponentName}",
                healthChecker.ComponentName
            );
            return await healthChecker.QuickHealthCheckAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error in quick check for component: {ComponentName}",
                healthChecker.ComponentName
            );
            return CreateErrorComponentHealth(healthChecker.ComponentName, ex.Message);
        }
    }

    private async Task<SystemMetrics> GetQuickSystemMetricsAsync(
        CancellationToken cancellationToken
    )
    {
        try
        {
            // Get only essential metrics for quick check
            var metrics = new SystemMetrics { SystemStartTime = DateTime.UtcNow };

            // Get recent usage (last 24 hours only)
            var recentUsage = await _usageTrackingService.GetUsageHistoryAsync(
                1,
                cancellationToken
            );
            var usageList = recentUsage.ToList();

            if (usageList.Any())
            {
                var successful = usageList.Where(u => u.WasSuccessful).ToList();
                metrics.TotalConversations = usageList.Count;
                metrics.SuccessRate = (double)successful.Count / usageList.Count * 100;
                metrics.ErrorsLast24Hours = usageList.Count(u => !u.WasSuccessful);
            }

            return metrics;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting quick system metrics");
            return new SystemMetrics { SystemStartTime = DateTime.UtcNow };
        }
    }

    private async Task<long> EstimateDatabaseSizeAsync(CancellationToken cancellationToken)
    {
        try
        {
            // Try to get database size from SQLite health checker
            var sqliteChecker = _healthCheckers.FirstOrDefault(hc => hc.ComponentName == "SQLite");
            if (sqliteChecker != null)
            {
                var metrics = await sqliteChecker.GetComponentMetricsAsync(cancellationToken);
                if (
                    metrics.TryGetValue("DatabaseSizeBytes", out var sizeObj)
                    && sizeObj is long size
                )
                {
                    return size;
                }
            }
            return 0;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not estimate database size");
            return 0;
        }
    }

    private async Task GenerateAlertsAndRecommendationsAsync(SystemHealthStatus systemHealth)
    {
        try
        {
            // Generate alerts for critical issues
            foreach (var component in systemHealth.Components)
            {
                if (component.Status == "Critical")
                {
                    systemHealth.AddAlert(
                        $"CRITICAL: {component.ComponentName} - {component.StatusMessage}"
                    );
                }
                else if (component.Status == "Warning")
                {
                    systemHealth.AddAlert(
                        $"WARNING: {component.ComponentName} - {component.StatusMessage}"
                    );
                }
            }

            // Generate system-level alerts
            if (systemHealth.Metrics.SuccessRate < 90.0)
            {
                systemHealth.AddAlert(
                    $"Low system success rate: {systemHealth.Metrics.SuccessRate:F1}%"
                );
            }

            if (systemHealth.Metrics.ErrorsLast24Hours > 20)
            {
                systemHealth.AddAlert(
                    $"High error count: {systemHealth.Metrics.ErrorsLast24Hours} errors in last 24 hours"
                );
            }

            // Get recommendations
            var recommendations = await GetHealthRecommendationsAsync(systemHealth);
            foreach (var recommendation in recommendations)
            {
                systemHealth.AddRecommendation(recommendation);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating alerts and recommendations");
        }
    }

    private static ComponentHealth CreateErrorComponentHealth(
        string componentName,
        string errorMessage
    )
    {
        return new ComponentHealth
        {
            ComponentName = componentName,
            Status = "Critical",
            StatusMessage = $"Health check failed: {errorMessage}",
            LastChecked = DateTime.UtcNow,
            IsConnected = false,
            ErrorCount = 1,
        };
    }

    private static SystemHealthStatus CreateErrorSystemHealth(string errorMessage)
    {
        var systemHealth = new SystemHealthStatus
        {
            OverallStatus = "Critical",
            LastChecked = DateTime.UtcNow,
            Metrics = new SystemMetrics
            {
                SystemStartTime = DateTime.UtcNow,
                ErrorsLast24Hours = 1,
            },
        };

        systemHealth.AddAlert($"System health check failed: {errorMessage}");
        systemHealth.AddRecommendation(
            "Check system logs and resolve underlying issues preventing health monitoring."
        );

        return systemHealth;
    }

    #endregion
}
