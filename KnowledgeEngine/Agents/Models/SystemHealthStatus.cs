using System.Text;

namespace KnowledgeEngine.Agents.Models;

/// <summary>
/// Comprehensive system health status aggregating all components and metrics
/// </summary>
public class SystemHealthStatus
{
    /// <summary>
    /// Overall system health status: Healthy, Warning, Critical, Unknown
    /// </summary>
    public string OverallStatus { get; set; } = "Unknown";

    /// <summary>
    /// When the health check was last performed
    /// </summary>
    public DateTime LastChecked { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Health status of individual system components
    /// </summary>
    public List<ComponentHealth> Components { get; set; } = new();

    /// <summary>
    /// System performance and resource metrics
    /// </summary>
    public SystemMetrics Metrics { get; set; } = new();

    /// <summary>
    /// Active alerts and issues requiring attention
    /// </summary>
    public List<string> ActiveAlerts { get; set; } = new();

    /// <summary>
    /// System optimization and improvement recommendations
    /// </summary>
    public List<string> Recommendations { get; set; } = new();

    /// <summary>
    /// Formatted uptime based on system metrics
    /// </summary>
    public string FormattedUptime => Metrics.FormattedUptime;

    /// <summary>
    /// Quick health summary for display
    /// </summary>
    public string HealthSummary => GenerateHealthSummary();

    /// <summary>
    /// Number of healthy components
    /// </summary>
    public int HealthyComponents => Components.Count(c => c.IsHealthy);

    /// <summary>
    /// Number of components with warnings
    /// </summary>
    public int ComponentsWithWarnings => Components.Count(c => c.HasWarnings);

    /// <summary>
    /// Number of critical components
    /// </summary>
    public int CriticalComponents => Components.Count(c => c.IsCritical);

    /// <summary>
    /// Number of offline/unreachable components
    /// </summary>
    public int OfflineComponents => Components.Count(c => !c.IsConnected);

    /// <summary>
    /// Overall system health percentage (0-100)
    /// </summary>
    public double SystemHealthPercentage
    {
        get
        {
            if (!Components.Any()) return 0;
            
            var totalWeight = 0;
            foreach (var component in Components)
            {
                if (!component.IsConnected)
                {
                    totalWeight += 0; // Offline components contribute 0
                }
                else if (component.IsHealthy)
                {
                    totalWeight += 100; // Healthy connected components contribute 100
                }
                else if (component.HasWarnings)
                {
                    totalWeight += 60; // Warning components contribute 60
                }
                else if (component.IsCritical)
                {
                    totalWeight += 20; // Critical components contribute 20
                }
                else
                {
                    totalWeight += 0; // Unknown status contributes 0
                }
            }
            
            var maxPossibleWeight = Components.Count * 100;
            return maxPossibleWeight > 0 ? (double)totalWeight / maxPossibleWeight * 100 : 0;
        }
    }

    /// <summary>
    /// Formatted system health percentage
    /// </summary>
    public string FormattedHealthPercentage => $"{SystemHealthPercentage:F1}%";

    /// <summary>
    /// Determines if the system is operating normally
    /// </summary>
    public bool IsSystemHealthy => 
        OverallStatus.Equals("Healthy", StringComparison.OrdinalIgnoreCase) &&
        CriticalComponents == 0 &&
        SystemHealthPercentage >= 80;

    /// <summary>
    /// Determines if the system has warnings but is still operational
    /// </summary>
    public bool HasWarnings =>
        OverallStatus.Equals("Warning", StringComparison.OrdinalIgnoreCase) ||
        ComponentsWithWarnings > 0 ||
        ActiveAlerts.Any();

    /// <summary>
    /// Determines if the system is in critical state
    /// </summary>
    public bool IsCritical =>
        OverallStatus.Equals("Critical", StringComparison.OrdinalIgnoreCase) ||
        CriticalComponents > 0 ||
        SystemHealthPercentage < 50;

    /// <summary>
    /// Gets the component health for a specific component by name
    /// </summary>
    public ComponentHealth? GetComponentHealth(string componentName) =>
        Components.FirstOrDefault(c => c.ComponentName.Equals(componentName, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Adds an alert to the active alerts list
    /// </summary>
    public void AddAlert(string alert)
    {
        if (!string.IsNullOrWhiteSpace(alert) && !ActiveAlerts.Contains(alert))
        {
            ActiveAlerts.Add(alert);
        }
    }

    /// <summary>
    /// Adds a recommendation to the recommendations list
    /// </summary>
    public void AddRecommendation(string recommendation)
    {
        if (!string.IsNullOrWhiteSpace(recommendation) && !Recommendations.Contains(recommendation))
        {
            Recommendations.Add(recommendation);
        }
    }

    /// <summary>
    /// Updates the overall status based on component health
    /// </summary>
    public void UpdateOverallStatus()
    {
        if (CriticalComponents > 0 || OfflineComponents > Components.Count / 2)
        {
            OverallStatus = "Critical";
        }
        else if (ComponentsWithWarnings > 0 || OfflineComponents > 0 || SystemHealthPercentage < 80)
        {
            OverallStatus = "Warning";
        }
        else if (HealthyComponents == Components.Count && Components.Any())
        {
            OverallStatus = "Healthy";
        }
        else
        {
            OverallStatus = "Unknown";
        }
    }

    /// <summary>
    /// Generates a concise health summary
    /// </summary>
    private string GenerateHealthSummary()
    {
        var summary = new StringBuilder();
        
        summary.Append($"Overall: {OverallStatus}");
        
        if (Components.Any())
        {
            summary.Append($" | {HealthyComponents}/{Components.Count} components healthy");
        }
        
        if (ActiveAlerts.Any())
        {
            summary.Append($" | {ActiveAlerts.Count} active alerts");
        }
        
        if (Metrics.ErrorsLast24Hours > 0)
        {
            summary.Append($" | {Metrics.ErrorsLast24Hours} errors (24h)");
        }

        return summary.ToString();
    }

    /// <summary>
    /// Gets components grouped by their status
    /// </summary>
    public Dictionary<string, List<ComponentHealth>> GetComponentsByStatus()
    {
        return Components
            .GroupBy(c => c.Status)
            .ToDictionary(g => g.Key, g => g.ToList());
    }

    /// <summary>
    /// Gets the worst (most severe) component status
    /// </summary>
    public string GetWorstComponentStatus()
    {
        if (Components.Any(c => c.IsCritical)) return "Critical";
        if (Components.Any(c => !c.IsConnected)) return "Offline";
        if (Components.Any(c => c.HasWarnings)) return "Warning";
        if (Components.Any(c => c.IsHealthy)) return "Healthy";
        return "Unknown";
    }
}