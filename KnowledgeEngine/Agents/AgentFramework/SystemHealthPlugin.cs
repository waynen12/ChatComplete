using System.ComponentModel;
using KnowledgeEngine.Agents.Models;
using KnowledgeEngine.Services;
using Microsoft.Extensions.Logging;

namespace KnowledgeEngine.Agents.AgentFramework;

/// <summary>
/// Agent Framework plugin for comprehensive system health monitoring and reporting.
/// Provides intelligent health insights and recommendations.
/// Migrated from Semantic Kernel SystemHealthAgent.
/// </summary>
public sealed class SystemHealthPlugin
{
    private readonly ISystemHealthService _systemHealthService;
    private readonly ILogger<SystemHealthPlugin> _logger;

    public SystemHealthPlugin(ISystemHealthService systemHealthService, ILogger<SystemHealthPlugin> logger)
    {
        _systemHealthService = systemHealthService;
        _logger = logger;
    }

    [Description(
        "Get comprehensive system health overview when users ask about overall system status, health checks, or general service availability. Use this for queries like 'How is the system?', 'System health check', 'Are there any issues?', or 'Overall status'. This provides a complete health report across all components. DO NOT use for specific component queries (use CheckComponentHealthAsync) or just performance metrics (use GetSystemMetricsAsync)."
        )]
    public async Task<string> GetSystemHealthAsync(
        [Description("Include detailed performance metrics and component analysis")] bool includeDetailedMetrics = true,
        [Description("Check specific component: all, critical-only, database, vector-store, ai-services")] string scope = "all",
        [Description("Include actionable recommendations for issues found")] bool includeRecommendations = true
    )
    {
        try
        {
            _logger.LogInformation("[AF] SystemHealthPlugin: Getting system health - scope: {Scope}, detailed: {Detailed}",
                scope, includeDetailedMetrics);

            SystemHealthStatus healthStatus;

            // Choose health check method based on scope
            switch (scope.ToLowerInvariant())
            {
                case "critical-only":
                case "quick":
                    healthStatus = await _systemHealthService.GetQuickHealthCheckAsync();
                    break;

                case "all":
                default:
                    healthStatus = await _systemHealthService.GetSystemHealthAsync();
                    break;
            }

            // Filter components based on scope if specified
            if (scope.ToLowerInvariant() != "all" && scope.ToLowerInvariant() != "critical-only")
            {
                var filteredComponents = FilterComponentsByScope(healthStatus.Components, scope);
                healthStatus.Components.Clear();
                healthStatus.Components.AddRange(filteredComponents);
                healthStatus.UpdateOverallStatus();
            }

            return FormatSystemHealthReport(healthStatus, includeDetailedMetrics, includeRecommendations);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[AF] Error getting system health");
            return $"❌ **System Health Check Failed**\n\nError: {ex.Message}\n\n" +
                   "Please check system logs and ensure all services are properly configured.";
        }
    }

    [Description(
        "Check the health of a specific system component when users ask about individual services or component status. Use this for queries like 'Is OpenAI working?', 'Check SQLite status', 'How is Qdrant doing?', 'Ollama health', 'component health', or 'service status'. Supports components: SQLite, Qdrant, Ollama, OpenAI, Anthropic, Google AI. DO NOT use for overall system status (use GetSystemHealthAsync) or general performance questions."
    )]
    public async Task<string> CheckComponentHealthAsync(
        [Description("Component name to check (e.g., SQLite, Qdrant, Ollama, OpenAI)")] string componentName,
        [Description("Include detailed metrics and diagnostic information")] bool includeMetrics = true
    )
    {
        try
        {
            _logger.LogInformation("[AF] SystemHealthPlugin: Checking component health - {ComponentName}", componentName);

            var componentHealth = await _systemHealthService.CheckComponentHealthAsync(componentName);
            return FormatComponentHealthReport(componentHealth, includeMetrics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[AF] Error checking component health for {ComponentName}", componentName);
            return $"❌ **Component Health Check Failed: {componentName}**\n\n" +
                   $"Error: {ex.Message}\n\n" +
                   "This component may not exist or may not have a health checker configured.";
        }
    }

    [Description(
        "Get system performance metrics and resource utilization when users ask about performance, speed, resource usage, or technical statistics. Use this for queries like 'System performance', 'How fast is the system?', 'Resource usage', 'Performance metrics', or 'System statistics'. DO NOT use for health status questions (use GetSystemHealthAsync) or specific component issues."
    )]
    public async Task<string> GetSystemMetricsAsync(
        [Description("Include formatted human-readable metrics")] bool includeFormatted = true,
        [Description("Focus area: performance, resources, usage, all")] string focus = "all"
    )
    {
        try
        {
            _logger.LogInformation("[AF] SystemHealthPlugin: Getting system metrics - focus: {Focus}", focus);

            var metrics = await _systemHealthService.GetSystemMetricsAsync();
            return FormatSystemMetricsReport(metrics, includeFormatted, focus);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[AF] Error getting system metrics");
            return $"❌ **System Metrics Collection Failed**\n\nError: {ex.Message}";
        }
    }

    [Description(
        "Get intelligent recommendations for improving system health and performance when users ask for suggestions, optimizations, or how to fix issues. Use this for queries like 'How can I improve performance?', 'What should I optimize?', 'Any recommendations?', 'How to fix issues?', or 'System improvements'. DO NOT use for current status (use GetSystemHealthAsync)."
        )]
    public async Task<string> GetHealthRecommendationsAsync(
        [Description("Include both immediate actions and long-term improvements")] bool includeLongTerm = true
    )
    {
        try
        {
            _logger.LogInformation("[AF] SystemHealthPlugin: Getting health recommendations");

            var healthStatus = await _systemHealthService.GetSystemHealthAsync();
            var recommendations = await _systemHealthService.GetHealthRecommendationsAsync(healthStatus);

            return FormatRecommendationsReport(recommendations, healthStatus, includeLongTerm);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[AF] Error getting health recommendations");
            return $"❌ **Recommendations Generation Failed**\n\nError: {ex.Message}";
        }
    }

    [Description(
        "List all system components that can be monitored when users ask what can be checked or what services are available for monitoring. Use this for queries like 'What components can you monitor?', 'List available services', 'What can you check?', or 'Show me all components'. DO NOT use for actual health status of components."
    )]
    public async Task<string> GetAvailableComponentsAsync()
    {
        try
        {
            _logger.LogInformation("[AF] SystemHealthPlugin: Getting available components");

            var components = await _systemHealthService.GetAvailableComponentsAsync();
            return FormatAvailableComponentsReport(components);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[AF] Error getting available components");
            return $"❌ **Component Discovery Failed**\n\nError: {ex.Message}";
        }
    }

    [Description(
        "Get a brief system health summary when users want a quick status check or dashboard view. Use this for queries like 'Quick status', 'Brief health check', 'Dashboard view', 'Summary of health', or when users want fast results without detailed analysis. DO NOT use for detailed diagnostics (use GetSystemHealthAsync) or specific component checks."
    )]
    public async Task<string> GetQuickHealthOverviewAsync()
    {
        try
        {
            _logger.LogInformation("[AF] SystemHealthPlugin: Getting quick health overview");

            var healthStatus = await _systemHealthService.GetQuickHealthCheckAsync();
            return FormatQuickHealthOverview(healthStatus);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[AF] Error getting quick health overview");
            return $"❌ **Quick Health Check Failed**\n\nError: {ex.Message}";
        }
    }

    #region Private Formatting Methods

    private static List<ComponentHealth> FilterComponentsByScope(List<ComponentHealth> components, string scope)
    {
        return scope.ToLowerInvariant() switch
        {
            "database" => components.Where(c => c.ComponentName.ToLowerInvariant().Contains("sqlite") ||
                                              c.ComponentName.ToLowerInvariant().Contains("database")).ToList(),
            "vector-store" => components.Where(c => c.ComponentName.ToLowerInvariant().Contains("qdrant") ||
                                                  c.ComponentName.ToLowerInvariant().Contains("vector")).ToList(),
            "ai-services" => components.Where(c => c.ComponentName.ToLowerInvariant().Contains("ollama") ||
                                                 c.ComponentName.ToLowerInvariant().Contains("openai") ||
                                                 c.ComponentName.ToLowerInvariant().Contains("anthropic") ||
                                                 c.ComponentName.ToLowerInvariant().Contains("gemini")).ToList(),
            _ => components
        };
    }

    private static string FormatSystemHealthReport(SystemHealthStatus healthStatus, bool includeDetailedMetrics, bool includeRecommendations)
    {
        var report = new System.Text.StringBuilder();

        // Header with overall status
        var statusIcon = GetStatusIcon(healthStatus.OverallStatus);
        report.AppendLine($"{statusIcon} **System Health Status: {healthStatus.OverallStatus}**");
        report.AppendLine($"📊 Health Score: {healthStatus.SystemHealthPercentage:F1}%");
        report.AppendLine($"🕒 Last Checked: {healthStatus.LastChecked:yyyy-MM-dd HH:mm:ss} UTC");
        report.AppendLine();

        // Component Summary
        report.AppendLine("## 📋 Component Summary");
        report.AppendLine($"- ✅ Healthy: {healthStatus.HealthyComponents}");
        report.AppendLine($"- ⚠️ Warnings: {healthStatus.ComponentsWithWarnings}");
        report.AppendLine($"- ❌ Critical: {healthStatus.CriticalComponents}");
        report.AppendLine($"- 🔴 Offline: {healthStatus.OfflineComponents}");
        report.AppendLine();

        // Component Details
        if (healthStatus.Components.Any())
        {
            report.AppendLine("## 🔧 Component Details");

            var sortedComponents = healthStatus.Components
                .OrderBy(c => GetStatusPriority(c.Status))
                .ThenBy(c => c.ComponentName);

            foreach (var component in sortedComponents)
            {
                var icon = GetStatusIcon(component.Status);
                report.AppendLine($"### {icon} {component.ComponentName}");
                report.AppendLine($"**Status:** {component.Status}");

                if (!string.IsNullOrEmpty(component.StatusMessage))
                {
                    report.AppendLine($"**Message:** {component.StatusMessage}");
                }

                if (component.ResponseTime.TotalMilliseconds > 0)
                {
                    report.AppendLine($"**Response Time:** {component.FormattedResponseTime}");
                }

                if (includeDetailedMetrics && component.Metrics.Any())
                {
                    report.AppendLine("**Metrics:**");
                    foreach (var metric in component.Metrics)
                    {
                        report.AppendLine($"- {metric.Key}: {metric.Value}");
                    }
                }

                report.AppendLine();
            }
        }

        // System Metrics
        if (includeDetailedMetrics)
        {
            report.AppendLine("## 📈 System Metrics");
            var metrics = healthStatus.Metrics;

            report.AppendLine($"- **Success Rate:** {metrics.FormattedSuccessRate}");
            report.AppendLine($"- **Average Response Time:** {metrics.FormattedAverageResponseTime}");
            report.AppendLine($"- **Errors (24h):** {metrics.ErrorsLast24Hours}");
            report.AppendLine($"- **Total Conversations:** {metrics.TotalConversations:N0}");
            report.AppendLine($"- **Token Usage:** {metrics.FormattedTokenUsage}");
            report.AppendLine($"- **Database Size:** {metrics.FormattedDatabaseSize}");
            report.AppendLine($"- **System Uptime:** {metrics.FormattedUptime}");
            report.AppendLine();
        }

        // Active Alerts
        if (healthStatus.ActiveAlerts.Any())
        {
            report.AppendLine("## 🚨 Active Alerts");
            foreach (var alert in healthStatus.ActiveAlerts)
            {
                report.AppendLine($"- {alert}");
            }
            report.AppendLine();
        }

        // Recommendations
        if (includeRecommendations && healthStatus.Recommendations.Any())
        {
            report.AppendLine("## 💡 Recommendations");
            foreach (var recommendation in healthStatus.Recommendations)
            {
                report.AppendLine($"- {recommendation}");
            }
            report.AppendLine();
        }

        // Health Summary
        if (healthStatus.IsSystemHealthy)
        {
            report.AppendLine("✅ **System is operating within normal parameters.**");
        }
        else
        {
            report.AppendLine("⚠️ **System requires attention. Please review the issues above.**");
        }

        return report.ToString();
    }

    private static string FormatComponentHealthReport(ComponentHealth componentHealth, bool includeMetrics)
    {
        var report = new System.Text.StringBuilder();

        var statusIcon = GetStatusIcon(componentHealth.Status);
        report.AppendLine($"{statusIcon} **{componentHealth.ComponentName} Health Status**");
        report.AppendLine();

        report.AppendLine($"**Status:** {componentHealth.Status}");
        report.AppendLine($"**Connected:** {(componentHealth.IsConnected ? "✅ Yes" : "❌ No")}");

        if (!string.IsNullOrEmpty(componentHealth.StatusMessage))
        {
            report.AppendLine($"**Message:** {componentHealth.StatusMessage}");
        }

        if (componentHealth.ResponseTime.TotalMilliseconds > 0)
        {
            report.AppendLine($"**Response Time:** {componentHealth.FormattedResponseTime}");
        }

        report.AppendLine($"**Last Checked:** {componentHealth.LastChecked:yyyy-MM-dd HH:mm:ss} UTC");

        if (componentHealth.ErrorCount > 0)
        {
            report.AppendLine($"**Error Count:** {componentHealth.ErrorCount}");
        }

        if (includeMetrics && componentHealth.Metrics.Any())
        {
            report.AppendLine();
            report.AppendLine("**Detailed Metrics:**");
            foreach (var metric in componentHealth.Metrics)
            {
                report.AppendLine($"- {metric.Key}: {metric.Value}");
            }
        }

        return report.ToString();
    }

    private static string FormatSystemMetricsReport(SystemMetrics metrics, bool includeFormatted, string focus)
    {
        var report = new System.Text.StringBuilder();

        report.AppendLine("📊 **System Performance Metrics**");
        report.AppendLine();

        if (focus == "all" || focus == "performance")
        {
            report.AppendLine("## 🚀 Performance Metrics");
            report.AppendLine($"- **Success Rate:** {metrics.FormattedSuccessRate}");
            report.AppendLine($"- **Average Response Time:** {metrics.FormattedAverageResponseTime}");
            report.AppendLine($"- **Errors (24h):** {metrics.ErrorsLast24Hours}");
            report.AppendLine($"- **Performance Healthy:** {(metrics.IsPerformanceHealthy ? "✅ Yes" : "⚠️ No")}");
            report.AppendLine();
        }

        if (focus == "all" || focus == "resources")
        {
            report.AppendLine("## 💾 Resource Utilization");
            report.AppendLine($"- **Database Size:** {metrics.FormattedDatabaseSize}");
            report.AppendLine($"- **Current Memory:** {metrics.FormattedCurrentMemory}");
            report.AppendLine($"- **Peak Memory:** {metrics.FormattedPeakMemory}");
            report.AppendLine($"- **Active Connections:** {metrics.ActiveConnections}");
            report.AppendLine($"- **Resource Concerns:** {(metrics.HasResourceConcerns ? "⚠️ Yes" : "✅ No")}");
            report.AppendLine();
        }

        if (focus == "all" || focus == "usage")
        {
            report.AppendLine("## 📈 Usage Statistics");
            report.AppendLine($"- **Total Conversations:** {metrics.TotalConversations:N0}");
            report.AppendLine($"- **Token Usage:** {metrics.FormattedTokenUsage}");
            report.AppendLine($"- **Active Knowledge Bases:** {metrics.ActiveKnowledgeBases}");
            report.AppendLine($"- **Vector Collections:** {metrics.VectorStoreCollections}");
            report.AppendLine($"- **Estimated Monthly Cost:** {metrics.FormattedEstimatedCost}");
            report.AppendLine();
        }

        report.AppendLine($"📅 **System Uptime:** {metrics.FormattedUptime}");

        return report.ToString();
    }

    private static string FormatRecommendationsReport(List<string> recommendations, SystemHealthStatus healthStatus, bool includeLongTerm)
    {
        var report = new System.Text.StringBuilder();

        report.AppendLine("💡 **System Health Recommendations**");
        report.AppendLine($"Based on system analysis performed at {healthStatus.LastChecked:yyyy-MM-dd HH:mm:ss} UTC");
        report.AppendLine();

        if (!recommendations.Any())
        {
            report.AppendLine("✅ **No immediate issues detected.**");
            report.AppendLine("Your system is running optimally!");

            if (includeLongTerm)
            {
                report.AppendLine();
                report.AppendLine("## 🔮 Proactive Maintenance Suggestions");
                report.AppendLine("- Monitor system performance trends weekly");
                report.AppendLine("- Review and update AI models monthly");
                report.AppendLine("- Archive old conversation data quarterly");
                report.AppendLine("- Check for system updates and security patches");
            }
        }
        else
        {
            report.AppendLine("## 🚨 Immediate Actions Required");
            var immediateActions = recommendations.Where(r =>
                r.ToLowerInvariant().Contains("critical") ||
                r.ToLowerInvariant().Contains("not accessible") ||
                r.ToLowerInvariant().Contains("not running")).ToList();

            if (immediateActions.Any())
            {
                foreach (var action in immediateActions)
                {
                    report.AppendLine($"❗ {action}");
                }
                report.AppendLine();
            }

            report.AppendLine("## ⚠️ Recommended Improvements");
            var improvements = recommendations.Except(immediateActions).ToList();
            foreach (var improvement in improvements)
            {
                report.AppendLine($"- {improvement}");
            }

            if (includeLongTerm)
            {
                report.AppendLine();
                report.AppendLine("## 📋 Long-term Optimization");
                report.AppendLine("- Set up automated health monitoring alerts");
                report.AppendLine("- Implement data retention policies");
                report.AppendLine("- Consider scaling resources based on usage patterns");
                report.AppendLine("- Establish regular backup and recovery procedures");
            }
        }

        return report.ToString();
    }

    private static string FormatAvailableComponentsReport(List<string> components)
    {
        var report = new System.Text.StringBuilder();

        report.AppendLine("🔧 **Available System Components**");
        report.AppendLine($"Total components available for health monitoring: {components.Count}");
        report.AppendLine();

        if (components.Any())
        {
            var categorized = CategorizeComponents(components);

            foreach (var category in categorized)
            {
                report.AppendLine($"## {category.Key}");
                foreach (var component in category.Value)
                {
                    report.AppendLine($"- {component}");
                }
                report.AppendLine();
            }
        }
        else
        {
            report.AppendLine("⚠️ No health checkers are currently registered.");
            report.AppendLine("This may indicate a configuration issue.");
        }

        return report.ToString();
    }

    private static string FormatQuickHealthOverview(SystemHealthStatus healthStatus)
    {
        var statusIcon = GetStatusIcon(healthStatus.OverallStatus);
        var healthScore = healthStatus.SystemHealthPercentage;

        var overview = $"{statusIcon} **System Status: {healthStatus.OverallStatus}** " +
                      $"({healthScore:F0}% healthy)\n\n";

        if (healthStatus.CriticalComponents > 0)
        {
            overview += $"🚨 **{healthStatus.CriticalComponents} critical issue(s)** require immediate attention\n";
        }

        if (healthStatus.ComponentsWithWarnings > 0)
        {
            overview += $"⚠️ **{healthStatus.ComponentsWithWarnings} warning(s)** detected\n";
        }

        if (healthStatus.HealthyComponents > 0)
        {
            overview += $"✅ **{healthStatus.HealthyComponents} component(s)** operating normally\n";
        }

        if (healthStatus.ActiveAlerts.Any())
        {
            overview += $"\n🔔 **{healthStatus.ActiveAlerts.Count} active alert(s)**\n";
        }

        return overview;
    }

    private static Dictionary<string, List<string>> CategorizeComponents(List<string> components)
    {
        var categorized = new Dictionary<string, List<string>>
        {
            ["🗄️ Database Components"] = new(),
            ["🧠 AI Services"] = new(),
            ["🔍 Vector Stores"] = new(),
            ["🌐 External APIs"] = new(),
            ["🔧 Other Services"] = new()
        };

        foreach (var component in components.OrderBy(c => c))
        {
            var lower = component.ToLowerInvariant();

            if (lower.Contains("sqlite") || lower.Contains("database"))
            {
                categorized["🗄️ Database Components"].Add(component);
            }
            else if (lower.Contains("ollama") || lower.Contains("local"))
            {
                categorized["🧠 AI Services"].Add(component);
            }
            else if (lower.Contains("qdrant") || lower.Contains("vector"))
            {
                categorized["🔍 Vector Stores"].Add(component);
            }
            else if (lower.Contains("openai") || lower.Contains("anthropic") || lower.Contains("gemini"))
            {
                categorized["🌐 External APIs"].Add(component);
            }
            else
            {
                categorized["🔧 Other Services"].Add(component);
            }
        }

        // Remove empty categories
        return categorized.Where(kv => kv.Value.Any()).ToDictionary(kv => kv.Key, kv => kv.Value);
    }

    private static string GetStatusIcon(string status)
    {
        return status.ToLowerInvariant() switch
        {
            "healthy" => "✅",
            "warning" => "⚠️",
            "critical" => "❌",
            "offline" => "🔴",
            _ => "❓"
        };
    }

    private static int GetStatusPriority(string status)
    {
        return status.ToLowerInvariant() switch
        {
            "critical" => 1,
            "offline" => 2,
            "warning" => 3,
            "healthy" => 4,
            _ => 5
        };
    }

    #endregion
}
