using Knowledge.Analytics.Models;
using Knowledge.Contracts.Types;
using Knowledge.Data.Interfaces;
using Microsoft.Extensions.Logging;

namespace Knowledge.Analytics.Services;

/// <summary>
/// Implementation of Ollama-specific analytics and metrics aggregation
/// </summary>
public class OllamaAnalyticsService : IOllamaAnalyticsService
{
    private readonly IOllamaRepository _ollamaRepository;
    private readonly IUsageMetricsRepository _usageRepository;
    private readonly ILogger<OllamaAnalyticsService> _logger;

    public OllamaAnalyticsService(
        IOllamaRepository ollamaRepository,
        IUsageMetricsRepository usageRepository,
        ILogger<OllamaAnalyticsService> logger)
    {
        _ollamaRepository = ollamaRepository;
        _usageRepository = usageRepository;
        _logger = logger;
    }

    public async Task<OllamaUsageInfo> GetUsageInfoAsync(int days = 30, CancellationToken cancellationToken = default)
    {
        try
        {
            var periodStart = DateTime.UtcNow.AddDays(-days);
            var periodEnd = DateTime.UtcNow;

            // Get all models and usage metrics in parallel
            var modelsTask = _ollamaRepository.GetInstalledModelsAsync(cancellationToken);
            var usageTask = _usageRepository.GetUsageHistoryAsync(days, cancellationToken);

            await Task.WhenAll(modelsTask, usageTask);

            var models = await modelsTask;
            var allUsage = await usageTask;
            
            // Filter usage to only Ollama provider
            var usage = allUsage.Where(u => u.Provider == "Ollama").ToList();

            // Calculate aggregate metrics
            var totalRequests = usage.Count();
            var successfulRequests = usage.Count(u => u.WasSuccessful);
            var totalTokens = usage.Sum(u => u.InputTokens + u.OutputTokens);
            var averageResponseTime = usage.Any() 
                ? usage.Average(u => u.ResponseTimeMs) 
                : 0;

            // Get top models by usage
            var topModels = usage
                .GroupBy(u => u.ModelName)
                .Select(g => new OllamaModelUsage
                {
                    ModelName = g.Key ?? "Unknown",
                    Requests = g.Count(),
                    TotalTokens = g.Sum(u => u.InputTokens + u.OutputTokens),
                    AverageResponseTimeMs = g.Average(u => u.ResponseTimeMs),
                    SizeBytes = models.FirstOrDefault(m => m.Name == g.Key)?.Size ?? 0,
                    SupportsTools = models.FirstOrDefault(m => m.Name == g.Key)?.SupportsTools ?? false,
                    LastUsed = g.Max(u => u.Timestamp)
                })
                .OrderByDescending(m => m.Requests)
                .Take(5);

            // Get recent downloads
            var recentDownloads = await GetRecentDownloadActivity(cancellationToken);

            return new OllamaUsageInfo
            {
                IsConnected = models.Any(), // Ollama is "connected" if we have models
                TotalModels = models.Count(),
                TotalDiskSpaceBytes = models.Sum(m => m.Size),
                TotalRequests = totalRequests,
                TotalTokens = totalTokens,
                AverageResponseTimeMs = averageResponseTime,
                SuccessRate = totalRequests > 0 ? (double)successfulRequests / totalRequests : 1.0,
                ToolEnabledModels = models.Count(m => m.SupportsTools == true),
                TopModels = topModels,
                RecentDownloads = recentDownloads,
                PeriodStart = periodStart,
                PeriodEnd = periodEnd
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get Ollama usage info for {Days} days", days);
            
            // Return basic info even on error
            return new OllamaUsageInfo
            {
                IsConnected = false,
                PeriodStart = DateTime.UtcNow.AddDays(-days),
                PeriodEnd = DateTime.UtcNow
            };
        }
    }

    public async Task<OllamaModelInventory> GetModelInventoryAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var models = await _ollamaRepository.GetInstalledModelsAsync(cancellationToken);

            var modelInfos = models.Select(m => new OllamaModelInfo
            {
                Name = m.Name,
                DisplayName = m.DisplayName,
                SizeBytes = m.Size,
                Family = m.Family,
                ParameterSize = m.ParameterSize,
                IsAvailable = m.IsAvailable,
                SupportsTools = m.SupportsTools ?? false,
                InstalledAt = m.InstalledAt,
                LastUsedAt = m.LastUsedAt
            });

            var modelsByFamily = models
                .Where(m => !string.IsNullOrEmpty(m.Family))
                .GroupBy(m => m.Family!)
                .ToDictionary(g => g.Key, g => g.Count());

            return new OllamaModelInventory
            {
                TotalModels = models.Count(),
                TotalSizeBytes = models.Sum(m => m.Size),
                AvailableModels = models.Count(m => m.IsAvailable),
                ToolEnabledModels = models.Count(m => m.SupportsTools == true),
                Models = modelInfos,
                ModelsByFamily = modelsByFamily,
                LastSyncAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get Ollama model inventory");
            return new OllamaModelInventory
            {
                LastSyncAt = DateTime.UtcNow
            };
        }
    }

    public async Task<OllamaDownloadStats> GetDownloadStatsAsync(int days = 30, CancellationToken cancellationToken = default)
    {
        try
        {
            var periodStart = DateTime.UtcNow.AddDays(-days);
            var downloads = await _ollamaRepository.GetDownloadHistoryAsync(periodStart, cancellationToken);

            var downloadInfos = downloads.Select(d => new OllamaDownloadInfo
            {
                ModelName = d.ModelName,
                Status = d.Status,
                TotalBytes = d.TotalBytes,
                StartedAt = d.StartedAt,
                CompletedAt = d.CompletedAt,
                ErrorMessage = d.ErrorMessage
            });

            var completedDownloads = downloads.Where(d => d.Status == "Completed").ToList();
            var averageDownloadTime = completedDownloads.Any() && completedDownloads.All(d => d.CompletedAt.HasValue)
                ? completedDownloads.Average(d => (d.CompletedAt!.Value - d.StartedAt).TotalMinutes)
                : 0;

            return new OllamaDownloadStats
            {
                TotalDownloads = downloads.Count(),
                CompletedDownloads = downloads.Count(d => d.Status == "Completed"),
                FailedDownloads = downloads.Count(d => d.Status == "Failed"),
                TotalBytesDownloaded = downloads.Where(d => d.Status == "Completed").Sum(d => d.TotalBytes),
                AverageDownloadTimeMinutes = averageDownloadTime,
                RecentDownloads = downloadInfos.OrderByDescending(d => d.StartedAt).Take(10),
                PeriodStart = periodStart,
                PeriodEnd = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get Ollama download stats for {Days} days", days);
            return new OllamaDownloadStats
            {
                PeriodStart = DateTime.UtcNow.AddDays(-days),
                PeriodEnd = DateTime.UtcNow
            };
        }
    }

    public async Task<IEnumerable<OllamaModelPerformance>> GetModelPerformanceAsync(int days = 30, CancellationToken cancellationToken = default)
    {
        try
        {
            var periodStart = DateTime.UtcNow.AddDays(-days);
            var allUsage = await _usageRepository.GetUsageHistoryAsync(days, cancellationToken);
            var usage = allUsage.Where(u => u.Provider == "Ollama").ToList();

            var models = await _ollamaRepository.GetInstalledModelsAsync(cancellationToken);
            var modelLookup = models.ToDictionary(m => m.Name, m => m);

            return usage
                .GroupBy(u => u.ModelName)
                .Select(g => new OllamaModelPerformance
                {
                    ModelName = g.Key ?? "Unknown",
                    Requests = g.Count(),
                    AverageResponseTimeMs = g.Average(u => u.ResponseTimeMs),
                    SuccessRate = (double)g.Count(u => u.WasSuccessful) / g.Count(),
                    TotalTokens = g.Sum(u => u.InputTokens + u.OutputTokens),
                    SupportsTools = modelLookup.TryGetValue(g.Key ?? "", out var model) ? model.SupportsTools ?? false : false,
                    LastUsed = g.Max(u => u.Timestamp)
                })
                .OrderByDescending(p => p.Requests);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get Ollama model performance for {Days} days", days);
            return [];
        }
    }

    private async Task<OllamaDownloadActivity> GetRecentDownloadActivity(CancellationToken cancellationToken)
    {
        try
        {
            var today = DateTime.UtcNow.Date;
            var downloads = await _ollamaRepository.GetDownloadHistoryAsync(today.AddDays(-7), cancellationToken);

            var recentDownloads = downloads
                .OrderByDescending(d => d.StartedAt)
                .Take(5)
                .Select(d => new OllamaDownloadInfo
                {
                    ModelName = d.ModelName,
                    Status = d.Status,
                    TotalBytes = d.TotalBytes,
                    StartedAt = d.StartedAt,
                    CompletedAt = d.CompletedAt,
                    ErrorMessage = d.ErrorMessage
                });

            return new OllamaDownloadActivity
            {
                PendingDownloads = downloads.Count(d => d.Status == "Pending" || d.Status == "Downloading"),
                CompletedToday = downloads.Count(d => d.Status == "Completed" && d.StartedAt >= today),
                FailedToday = downloads.Count(d => d.Status == "Failed" && d.StartedAt >= today),
                RecentDownloads = recentDownloads
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get recent download activity");
            return new OllamaDownloadActivity();
        }
    }
}