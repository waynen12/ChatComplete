using KnowledgeEngine.Services;
using KnowledgeEngine.Persistence.VectorStores;
using ModelContextProtocol;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;

namespace Knowledge.Mcp.Tools;

/// <summary>
/// System Health MCP Tool - Provides real system status information via Model Context Protocol
/// Integrates with existing KnowledgeEngine SystemHealthService for comprehensive health checking
/// </summary>
[McpServerToolType]
public sealed class SystemHealthMcpTool
{
    /// <summary>
    /// Gets comprehensive system health status - the main system health method called by Claude Code.
    /// Maps to the existing get_system_health MCP function name for compatibility.
    /// </summary>
    /// <param name="serviceProvider">Dependency injection service provider to resolve ISystemHealthService</param>
    /// <returns>JSON-formatted health status with real system data</returns>
    [McpServerTool]
    [Description("Check the overall system health status of the Knowledge Manager")]
    public static async Task<string> GetSystemHealthAsync(
        [Description("Service provider for dependency injection")] IServiceProvider serviceProvider)
    {
        try
        {
            // Resolve the system health service from DI container
            var systemHealthService = serviceProvider.GetRequiredService<ISystemHealthService>();
            
            // Get comprehensive system health check
            var healthStatus = await systemHealthService.GetSystemHealthAsync();

            // Create MCP-friendly response format matching the existing interface
            var mcpResponse = new
            {
                Status = healthStatus.OverallStatus,
                HealthScore = Math.Round(healthStatus.SystemHealthPercentage, 1),
                Timestamp = healthStatus.LastChecked,
                Summary = new
                {
                    HealthyComponents = healthStatus.HealthyComponents,
                    ComponentsWithWarnings = healthStatus.ComponentsWithWarnings,
                    CriticalComponents = healthStatus.CriticalComponents,
                    OfflineComponents = healthStatus.OfflineComponents,
                    TotalComponents = healthStatus.Components.Count
                },
                IsSystemHealthy = healthStatus.IsSystemHealthy,
                ActiveAlerts = healthStatus.ActiveAlerts.Take(5).ToList(),
                QuickRecommendations = healthStatus.Recommendations.Take(3).ToList(),
                Components = healthStatus.Components.ToDictionary(
                    c => c.ComponentName,
                    c => new
                    {
                        Status = c.Status,
                        IsConnected = c.IsConnected,
                        ResponseTime = c.FormattedResponseTime,
                        LastChecked = c.LastChecked,
                        ErrorCount = c.ErrorCount
                    }
                ),
                Metrics = new
                {
                    SuccessRate = healthStatus.Metrics.FormattedSuccessRate,
                    AverageResponseTime = healthStatus.Metrics.FormattedAverageResponseTime,
                    ErrorsLast24Hours = healthStatus.Metrics.ErrorsLast24Hours,
                    SystemUptime = healthStatus.Metrics.FormattedUptime,
                    TotalConversations = healthStatus.Metrics.TotalConversations,
                    DatabaseSize = healthStatus.Metrics.FormattedDatabaseSize
                }
            };

            return JsonSerializer.Serialize(mcpResponse, new JsonSerializerOptions 
            { 
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
        }
        catch (Exception ex)
        {
            // Return error information in structured format
            var errorResponse = new
            {
                Status = "Error", 
                HealthScore = 0.0,
                Timestamp = DateTime.UtcNow,
                ErrorMessage = ex.Message,
                IsSystemHealthy = false,
                Summary = new
                {
                    HealthyComponents = 0,
                    ComponentsWithWarnings = 0,
                    CriticalComponents = 1,
                    OfflineComponents = 0,
                    TotalComponents = 0
                }
            };

            return JsonSerializer.Serialize(errorResponse, new JsonSerializerOptions 
            { 
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
        }
    }

    /// <summary>
    /// Performs a quick health check focusing on critical system components.
    /// Returns essential health information in a concise format suitable for MCP clients.
    /// </summary>
    /// <param name="serviceProvider">Dependency injection service provider to resolve ISystemHealthService</param>
    /// <returns>JSON-formatted health status with overall status, component counts, and key metrics</returns>
    [McpServerTool]
    [Description("Get a quick overview of system health status including critical components, overall health score, and component summary. Returns concise health information suitable for monitoring dashboards.")]
    public static async Task<string> GetQuickHealthOverviewAsync(
        [Description("Service provider for dependency injection")] IServiceProvider serviceProvider)
    {
        try
        {
            // Resolve the system health service from DI container
            var systemHealthService = serviceProvider.GetRequiredService<ISystemHealthService>();
            
            // Get quick health check (focuses on critical components)
            var healthStatus = await systemHealthService.GetQuickHealthCheckAsync();

            // Create MCP-friendly response format
            var mcpResponse = new
            {
                Status = healthStatus.OverallStatus,
                HealthScore = Math.Round(healthStatus.SystemHealthPercentage, 1),
                Timestamp = healthStatus.LastChecked,
                Summary = new
                {
                    HealthyComponents = healthStatus.HealthyComponents,
                    ComponentsWithWarnings = healthStatus.ComponentsWithWarnings,
                    CriticalComponents = healthStatus.CriticalComponents,
                    OfflineComponents = healthStatus.OfflineComponents,
                    TotalComponents = healthStatus.Components.Count
                },
                IsSystemHealthy = healthStatus.IsSystemHealthy,
                ActiveAlerts = healthStatus.ActiveAlerts.Take(3).ToList(), // Limit to 3 most critical
                QuickRecommendations = healthStatus.Recommendations.Take(2).ToList(), // Top 2 recommendations
                Metrics = new
                {
                    SuccessRate = healthStatus.Metrics.FormattedSuccessRate,
                    AverageResponseTime = healthStatus.Metrics.FormattedAverageResponseTime,
                    ErrorsLast24Hours = healthStatus.Metrics.ErrorsLast24Hours,
                    SystemUptime = healthStatus.Metrics.FormattedUptime
                }
            };

            return JsonSerializer.Serialize(mcpResponse, new JsonSerializerOptions 
            { 
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
        }
        catch (Exception ex)
        {
            // Return error information in structured format
            var errorResponse = new
            {
                Status = "Error",
                HealthScore = 0.0,
                Timestamp = DateTime.UtcNow,
                ErrorMessage = ex.Message,
                IsSystemHealthy = false,
                Summary = new
                {
                    HealthyComponents = 0,
                    ComponentsWithWarnings = 0,
                    CriticalComponents = 1,
                    OfflineComponents = 0,
                    TotalComponents = 0
                }
            };

            return JsonSerializer.Serialize(errorResponse, new JsonSerializerOptions 
            { 
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
        }
    }

    /// <summary>
    /// Debugs Qdrant configuration and connection details for troubleshooting.
    /// Shows actual configuration values and tests direct connection.
    /// </summary>
    /// <param name="serviceProvider">Dependency injection service provider</param>
    /// <returns>JSON-formatted debug information including configuration and test results</returns>
    [McpServerTool]
    [Description("Debug Qdrant configuration and connection. Shows actual config values and tests direct connection to help troubleshoot collection detection issues.")]
    public static async Task<string> DebugQdrantConfigAsync(
        [Description("Service provider for dependency injection")] IServiceProvider serviceProvider)
    {
        try
        {
            var chatSettings = serviceProvider.GetRequiredService<ChatCompletion.Config.ChatCompleteSettings>();
            var qdrantSettings = serviceProvider.GetRequiredService<ChatCompletion.Config.QdrantSettings>();
            var vectorStore = serviceProvider.GetRequiredService<Microsoft.SemanticKernel.Connectors.Qdrant.QdrantVectorStore>();
            var vectorStoreStrategy = serviceProvider.GetRequiredService<IVectorStoreStrategy>();

            // Test direct Qdrant client connection
            var collections = new List<string>();
            try
            {
                await foreach (var name in vectorStore.ListCollectionNamesAsync())
                {
                    collections.Add(name);
                }
            }
            catch (Exception ex)
            {
                collections.Add($"ERROR: {ex.Message}");
            }

            // Test strategy wrapper
            var strategyCollections = new List<string>();
            try
            {
                strategyCollections = await vectorStoreStrategy.ListCollectionsAsync();
            }
            catch (Exception ex)
            {
                strategyCollections.Add($"ERROR: {ex.Message}");
            }

            var debugInfo = new
            {
                Timestamp = DateTime.UtcNow,
                ConfigurationSources = new
                {
                    ChatSettingsVectorStoreProvider = chatSettings.VectorStore?.Provider,
                    QdrantSettingsFromDI = new
                    {
                        Host = qdrantSettings.Host,
                        Port = qdrantSettings.Port,
                        UseHttps = qdrantSettings.UseHttps,
                        ApiKey = string.IsNullOrEmpty(qdrantSettings.ApiKey) ? "null" : "***SET***"
                    }
                },
                ServiceRegistration = new
                {
                    VectorStoreType = vectorStore?.GetType().Name,
                    VectorStoreStrategyType = vectorStoreStrategy?.GetType().Name
                },
                ConnectionTests = new
                {
                    DirectVectorStoreCollections = collections,
                    DirectVectorStoreCount = collections.Count(c => !c.StartsWith("ERROR:")),
                    StrategyCollections = strategyCollections,
                    StrategyCount = strategyCollections.Count(c => !c.StartsWith("ERROR:"))
                },
                TroubleshootingInfo = new
                {
                    ExpectedPort = 6334,
                    ActualConfiguredPort = qdrantSettings.Port,
                    PortMatch = qdrantSettings.Port == 6334,
                    RestApiTest = "curl http://localhost:6333/collections",
                    GrpcPortNote = "Semantic Kernel uses gRPC (6334), REST API uses 6333"
                }
            };

            return JsonSerializer.Serialize(debugInfo, new JsonSerializerOptions 
            { 
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
        }
        catch (Exception ex)
        {
            var errorResponse = new
            {
                Timestamp = DateTime.UtcNow,
                Status = "DebugError",
                ErrorMessage = ex.Message,
                StackTrace = ex.StackTrace
            };

            return JsonSerializer.Serialize(errorResponse, new JsonSerializerOptions 
            { 
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
        }
    }

    /// <summary>
    /// Checks the health status of a specific system component.
    /// Useful for targeted diagnostics and component-specific troubleshooting.
    /// </summary>
    /// <param name="serviceProvider">Dependency injection service provider</param>
    /// <param name="componentName">Name of the component to check (e.g., SQLite, Qdrant, OpenAI, Ollama)</param>
    /// <returns>JSON-formatted component health details including status, metrics, and diagnostics</returns>
    [McpServerTool]
    [Description("Check the health of a specific system component. Supports components like SQLite, Qdrant, OpenAI, Anthropic, Ollama, and others. Returns detailed component status and metrics.")]
    public static async Task<string> CheckComponentHealthAsync(
        [Description("Service provider for dependency injection")] IServiceProvider serviceProvider,
        [Description("Component name to check (e.g., SQLite, Qdrant, OpenAI, Ollama)")] string componentName)
    {
        try
        {
            var systemHealthService = serviceProvider.GetRequiredService<ISystemHealthService>();
            var componentHealth = await systemHealthService.CheckComponentHealthAsync(componentName);

            var mcpResponse = new
            {
                ComponentName = componentHealth.ComponentName,
                Status = componentHealth.Status,
                IsConnected = componentHealth.IsConnected,
                StatusMessage = componentHealth.StatusMessage,
                ResponseTime = componentHealth.FormattedResponseTime,
                LastChecked = componentHealth.LastChecked,
                ErrorCount = componentHealth.ErrorCount,
                Metrics = componentHealth.Metrics.ToDictionary(m => m.Key, m => m.Value),
                IsHealthy = componentHealth.Status.Equals("Healthy", StringComparison.OrdinalIgnoreCase)
            };

            return JsonSerializer.Serialize(mcpResponse, new JsonSerializerOptions 
            { 
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
        }
        catch (Exception ex)
        {
            var errorResponse = new
            {
                ComponentName = componentName,
                Status = "Error",
                IsConnected = false,
                StatusMessage = $"Health check failed: {ex.Message}",
                ResponseTime = "N/A",
                LastChecked = DateTime.UtcNow,
                ErrorCount = 1,
                IsHealthy = false
            };

            return JsonSerializer.Serialize(errorResponse, new JsonSerializerOptions 
            { 
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
        }
    }
}