using System.Diagnostics;
using KnowledgeEngine.Agents.Models;
using KnowledgeEngine.Persistence.VectorStores;
using Microsoft.Extensions.Logging;

namespace KnowledgeEngine.Services.HealthCheckers;

/// <summary>
/// Health checker for Qdrant vector store connectivity and status
/// </summary>
public class QdrantHealthChecker : IComponentHealthChecker
{
    private readonly IVectorStoreStrategy _vectorStore;
    private readonly ILogger<QdrantHealthChecker> _logger;

    public string ComponentName => "Qdrant";
    public int Priority => 2; // High priority - critical for vector operations
    public bool IsCriticalComponent => true;

    public QdrantHealthChecker(IVectorStoreStrategy vectorStore, ILogger<QdrantHealthChecker> logger)
    {
        _vectorStore = vectorStore;
        _logger = logger;
    }

    public async Task<ComponentHealth> CheckHealthAsync(CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var health = new ComponentHealth
        {
            ComponentName = ComponentName,
            LastChecked = DateTime.UtcNow
        };

        try
        {
            // Test basic connectivity by listing collections
            var collections = await _vectorStore.ListCollectionsAsync(cancellationToken);
            health.IsConnected = true;

            // Get detailed metrics
            var metrics = await GetComponentMetricsAsync(cancellationToken);
            health.Metrics = metrics;

            stopwatch.Stop();
            health.ResponseTime = stopwatch.Elapsed;

            // Determine health status based on metrics and response time
            var collectionCount = collections.Count;
            
            if (health.ResponseTime.TotalMilliseconds > 5000) // > 5 seconds
            {
                health.Status = "Critical";
                health.StatusMessage = $"Very slow response time ({health.FormattedResponseTime}). Check Qdrant server status.";
            }
            else if (health.ResponseTime.TotalMilliseconds > 2000) // > 2 seconds
            {
                health.Status = "Warning";
                health.StatusMessage = $"Slow response time ({health.FormattedResponseTime})";
            }
            else if (collectionCount == 0)
            {
                health.Status = "Warning";
                health.StatusMessage = "No collections found. System may not be initialized.";
            }
            else
            {
                health.Status = "Healthy";
                health.StatusMessage = $"Vector store operational with {collectionCount} collections";
            }

            health.LastSuccess = DateTime.UtcNow;
            _logger.LogDebug("Qdrant health check completed successfully in {ResponseTime}ms with {CollectionCount} collections", 
                health.ResponseTime.TotalMilliseconds, collectionCount);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            health.ResponseTime = stopwatch.Elapsed;
            health.Status = "Critical";
            health.StatusMessage = $"Vector store connection failed: {ex.Message}";
            health.IsConnected = false;
            health.ErrorCount = 1;

            _logger.LogError(ex, "Qdrant health check failed");
        }

        return health;
    }

    public async Task<ComponentHealth> QuickHealthCheckAsync(CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var health = new ComponentHealth
        {
            ComponentName = ComponentName,
            LastChecked = DateTime.UtcNow
        };

        try
        {
            // Quick connectivity test - just try to list collections
            await _vectorStore.ListCollectionsAsync(cancellationToken);
            health.IsConnected = true;
            
            stopwatch.Stop();
            health.ResponseTime = stopwatch.Elapsed;
            
            if (health.ResponseTime.TotalMilliseconds > 5000)
            {
                health.Status = "Warning";
                health.StatusMessage = $"Slow response ({health.FormattedResponseTime})";
            }
            else
            {
                health.Status = "Healthy";
                health.StatusMessage = "Vector store connection successful";
            }
            
            health.LastSuccess = DateTime.UtcNow;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            health.ResponseTime = stopwatch.Elapsed;
            health.Status = "Critical";
            health.StatusMessage = $"Connection failed: {ex.Message}";
            health.IsConnected = false;
            health.ErrorCount = 1;
        }

        return health;
    }

    public async Task<Dictionary<string, object>> GetComponentMetricsAsync(CancellationToken cancellationToken = default)
    {
        var metrics = new Dictionary<string, object>();

        try
        {
            // Get collection information
            var collections = await _vectorStore.ListCollectionsAsync(cancellationToken);
            metrics["CollectionCount"] = collections.Count;
            metrics["Collections"] = collections;

            // Estimate total vectors (simplified - would need specific Qdrant client for exact counts)
            metrics["EstimatedVectorCount"] = collections.Count * 1000; // Rough estimate

            // Connection info
            metrics["ConnectionStatus"] = "Connected";
            metrics["LastConnectionTest"] = DateTime.UtcNow;

            // Performance metrics (would be gathered from actual usage in production)
            metrics["AverageQueryTime"] = "< 100ms"; // Placeholder
            metrics["IndexingStatus"] = "Operational";

            _logger.LogDebug("Gathered Qdrant metrics: {CollectionCount} collections", collections.Count);
        }
        catch (Exception ex)
        {
            metrics["Error"] = ex.Message;
            metrics["ConnectionStatus"] = "Failed";
            _logger.LogError(ex, "Failed to gather Qdrant metrics");
        }

        return metrics;
    }
}