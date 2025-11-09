using System.Diagnostics;
using System.Text.Json;
using KnowledgeEngine.Agents.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace KnowledgeEngine.Services.HealthCheckers;

/// <summary>
/// Health checker for Ollama service connectivity and model availability
/// </summary>
public class OllamaHealthChecker : IComponentHealthChecker
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<OllamaHealthChecker> _logger;
    private readonly string _ollamaBaseUrl;

    public string ComponentName => "Ollama";
    public int Priority => 3; // Medium-high priority - important for local AI capabilities
    public bool IsCriticalComponent => false; // Not critical if other providers are available

    public OllamaHealthChecker(HttpClient httpClient, IConfiguration configuration, ILogger<OllamaHealthChecker> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _ollamaBaseUrl = configuration["ChatCompleteSettings:OllamaBaseUrl"] ?? "http://localhost:11434";
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
            // Test connectivity by getting installed models
            var response = await _httpClient.GetAsync($"{_ollamaBaseUrl}/api/tags", cancellationToken);
            health.IsConnected = response.IsSuccessStatusCode;

            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"Ollama API returned {response.StatusCode}");
            }

            var jsonContent = await response.Content.ReadAsStringAsync(cancellationToken);
            var ollamaResponse = JsonSerializer.Deserialize<OllamaTagsResponse>(jsonContent, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower });
            var models = ollamaResponse?.Models ?? new List<OllamaModel>();

            // Get detailed metrics
            var metrics = await GetComponentMetricsAsync(cancellationToken);
            health.Metrics = metrics;

            stopwatch.Stop();
            health.ResponseTime = stopwatch.Elapsed;

            // Determine health status based on models and response time
            var modelCount = models.Count;
            
            if (health.ResponseTime.TotalMilliseconds > 10000) // > 10 seconds
            {
                health.Status = "Critical";
                health.StatusMessage = $"Very slow response time ({health.FormattedResponseTime}). Check Ollama service status.";
            }
            else if (health.ResponseTime.TotalMilliseconds > 5000) // > 5 seconds
            {
                health.Status = "Warning";
                health.StatusMessage = $"Slow response time ({health.FormattedResponseTime})";
            }
            else if (modelCount == 0)
            {
                health.Status = "Warning";
                health.StatusMessage = "No models installed. Download models to enable local AI capabilities.";
            }
            else
            {
                health.Status = "Healthy";
                health.StatusMessage = $"Ollama service operational with {modelCount} models available";
            }

            health.LastSuccess = DateTime.UtcNow;
            _logger.LogDebug("Ollama health check completed successfully in {ResponseTime}ms with {ModelCount} models", 
                health.ResponseTime.TotalMilliseconds, modelCount);
        }
        catch (HttpRequestException ex)
        {
            stopwatch.Stop();
            health.ResponseTime = stopwatch.Elapsed;
            health.Status = "Offline";
            health.StatusMessage = "Ollama service not reachable. Check if Ollama is running.";
            health.IsConnected = false;
            health.ErrorCount = 1;

            _logger.LogWarning(ex, "Ollama service not reachable");
        }
        catch (TaskCanceledException ex)
        {
            stopwatch.Stop();
            health.ResponseTime = stopwatch.Elapsed;
            health.Status = "Critical";
            health.StatusMessage = "Ollama service timeout. Service may be overloaded.";
            health.IsConnected = false;
            health.ErrorCount = 1;

            _logger.LogWarning(ex, "Ollama health check timed out");
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            health.ResponseTime = stopwatch.Elapsed;
            health.Status = "Critical";
            health.StatusMessage = $"Ollama service error: {ex.Message}";
            health.IsConnected = false;
            health.ErrorCount = 1;

            _logger.LogError(ex, "Ollama health check failed");
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
            // Quick connectivity test - just get models list
            var response = await _httpClient.GetAsync($"{_ollamaBaseUrl}/api/tags", cancellationToken);
            health.IsConnected = response.IsSuccessStatusCode;
            
            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"Ollama API returned {response.StatusCode}");
            }

            var jsonContent = await response.Content.ReadAsStringAsync(cancellationToken);
            var ollamaResponse = JsonSerializer.Deserialize<OllamaTagsResponse>(jsonContent, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower });
            var models = ollamaResponse?.Models ?? new List<OllamaModel>();
            
            stopwatch.Stop();
            health.ResponseTime = stopwatch.Elapsed;
            
            if (health.ResponseTime.TotalMilliseconds > 10000)
            {
                health.Status = "Warning";
                health.StatusMessage = $"Slow response ({health.FormattedResponseTime})";
            }
            else
            {
                health.Status = "Healthy";
                health.StatusMessage = $"Service accessible with {models.Count} models";
            }
            
            health.LastSuccess = DateTime.UtcNow;
        }
        catch (HttpRequestException)
        {
            stopwatch.Stop();
            health.ResponseTime = stopwatch.Elapsed;
            health.Status = "Offline";
            health.StatusMessage = "Service not reachable";
            health.IsConnected = false;
            health.ErrorCount = 1;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            health.ResponseTime = stopwatch.Elapsed;
            health.Status = "Critical";
            health.StatusMessage = $"Service error: {ex.Message}";
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
            // Get installed models
            var response = await _httpClient.GetAsync($"{_ollamaBaseUrl}/api/tags", cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"Ollama API returned {response.StatusCode}");
            }

            var jsonContent = await response.Content.ReadAsStringAsync(cancellationToken);
            var ollamaResponse = JsonSerializer.Deserialize<OllamaTagsResponse>(jsonContent, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower });
            var models = ollamaResponse?.Models ?? new List<OllamaModel>();

            metrics["ModelCount"] = models.Count;
            metrics["InstalledModels"] = models.Select(m => m.Name).ToList();

            // Calculate total model storage size
            var totalSize = models.Sum(m => m.Size);
            metrics["TotalModelSizeBytes"] = totalSize;
            metrics["FormattedTotalSize"] = FormatBytes(totalSize);

            // Model information
            if (models.Count > 0)
            {
                metrics["NewestModel"] = models.OrderByDescending(m => m.ModifiedAt).First().Name;
                metrics["OldestModel"] = models.OrderBy(m => m.ModifiedAt).First().Name;
                metrics["LargestModel"] = models.OrderByDescending(m => m.Size).First().Name;
            }

            // Service status
            metrics["ServiceStatus"] = "Running";
            metrics["LastModelCheck"] = DateTime.UtcNow;

            _logger.LogDebug("Gathered Ollama metrics: {ModelCount} models, {TotalSize} total size", 
                models.Count, FormatBytes(totalSize));
        }
        catch (Exception ex)
        {
            metrics["Error"] = ex.Message;
            metrics["ServiceStatus"] = "Error";
            _logger.LogError(ex, "Failed to gather Ollama metrics");
        }

        return metrics;
    }

    private static string FormatBytes(long bytes)
    {
        if (bytes == 0) return "0 bytes";

        string[] sizes = { "bytes", "KB", "MB", "GB", "TB" };
        int order = 0;
        double size = bytes;

        while (size >= 1024 && order < sizes.Length - 1)
        {
            order++;
            size /= 1024;
        }

        return $"{size:F1} {sizes[order]}";
    }
}

/// <summary>
/// Simple DTO for Ollama model information
/// </summary>
internal class OllamaModel
{
    public string Name { get; set; } = string.Empty;
    public long Size { get; set; }
    public DateTime ModifiedAt { get; set; }
}

/// <summary>
/// Response from Ollama's /api/tags endpoint
/// </summary>
internal class OllamaTagsResponse
{
    public List<OllamaModel> Models { get; set; } = new();
}