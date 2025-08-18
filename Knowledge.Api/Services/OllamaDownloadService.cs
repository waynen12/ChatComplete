using KnowledgeEngine.Persistence.Sqlite.Repositories;
using System.Collections.Concurrent;
using System.Text.Json;

namespace Knowledge.Api.Services;

/// <summary>
/// Service for managing Ollama model downloads with real-time progress tracking
/// Coordinates between API calls and database persistence
/// </summary>
public class OllamaDownloadService
{
    private readonly IOllamaApiService _ollamaApi;
    private readonly SqliteOllamaRepository _repository;
    private readonly ILogger<OllamaDownloadService> _logger;
    
    // Track active downloads for real-time updates
    private readonly ConcurrentDictionary<string, CancellationTokenSource> _activeDownloads = new();

    public OllamaDownloadService(
        IOllamaApiService ollamaApi, 
        SqliteOllamaRepository repository,
        ILogger<OllamaDownloadService> logger)
    {
        _ollamaApi = ollamaApi;
        _repository = repository;
        _logger = logger;
    }

    /// <summary>
    /// Starts a model download and tracks progress in the database
    /// </summary>
    public async Task<bool> StartDownloadAsync(string modelName, CancellationToken cancellationToken = default)
    {
        // Check if already downloading
        if (_activeDownloads.ContainsKey(modelName))
        {
            _logger.LogWarning("Model {ModelName} is already being downloaded", modelName);
            return false;
        }

        // Create cancellation token for this download
        var downloadCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _activeDownloads[modelName] = downloadCts;

        try
        {
            // Initialize download record
            await _repository.UpsertDownloadProgressAsync(new OllamaDownloadRecord
            {
                ModelName = modelName,
                Status = "Starting",
                BytesDownloaded = 0,
                TotalBytes = 0,
                PercentComplete = 0
            }, cancellationToken);

            // Start background download task
            _ = Task.Run(async () => await ProcessDownloadAsync(modelName, downloadCts.Token), cancellationToken);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start download for model {ModelName}", modelName);
            _activeDownloads.TryRemove(modelName, out _);
            downloadCts.Dispose();
            return false;
        }
    }

    /// <summary>
    /// Cancels an active download
    /// </summary>
    public async Task<bool> CancelDownloadAsync(string modelName)
    {
        if (_activeDownloads.TryRemove(modelName, out var cts))
        {
            cts.Cancel();
            cts.Dispose();

            // Update database status
            await _repository.UpsertDownloadProgressAsync(new OllamaDownloadRecord
            {
                ModelName = modelName,
                Status = "Cancelled",
                BytesDownloaded = 0,
                TotalBytes = 0,
                PercentComplete = 0,
                ErrorMessage = "Download cancelled by user"
            });

            _logger.LogInformation("Cancelled download for model {ModelName}", modelName);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Gets current download status from database
    /// </summary>
    public Task<OllamaDownloadRecord?> GetDownloadStatusAsync(string modelName, CancellationToken cancellationToken = default)
    {
        return _repository.GetDownloadStatusAsync(modelName, cancellationToken);
    }

    /// <summary>
    /// Gets all active downloads
    /// </summary>
    public Task<List<OllamaDownloadRecord>> GetActiveDownloadsAsync(CancellationToken cancellationToken = default)
    {
        return _repository.GetActiveDownloadsAsync(cancellationToken);
    }

    /// <summary>
    /// Streams download progress updates via Server-Sent Events
    /// </summary>
    public async IAsyncEnumerable<string> StreamDownloadProgressAsync(
        string modelName, 
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var progressEvent in StreamProgressInternal(modelName, cancellationToken))
        {
            yield return progressEvent;
        }
    }

    /// <summary>
    /// Internal method to handle the streaming progress with proper error handling.
    /// </summary>
    private async IAsyncEnumerable<string> StreamProgressInternal(
        string modelName, 
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var lastProgress = -1.0;
        var startTime = DateTime.UtcNow;

        while (!cancellationToken.IsCancellationRequested)
        {
            var status = await SafeGetDownloadStatus(modelName, cancellationToken);
            if (status == null)
            {
                // Download not found, may have been cleaned up
                yield return FormatSSE("complete", JsonSerializer.Serialize(new { 
                    model = modelName, 
                    status = "not_found",
                    message = "Download not found"
                }));
                yield break;
            }

            // Only send updates when progress changes or every 5 seconds
            var timeSinceStart = DateTime.UtcNow - startTime;
            var shouldSendUpdate = Math.Abs(status.PercentComplete - lastProgress) > 0.1 || 
                                 timeSinceStart.TotalSeconds % 5 < 1;

            if (shouldSendUpdate)
            {
                lastProgress = status.PercentComplete;
                
                var progressData = new
                {
                    model = status.ModelName,
                    status = status.Status.ToLowerInvariant(),
                    bytesDownloaded = status.BytesDownloaded,
                    totalBytes = status.TotalBytes,
                    percentComplete = Math.Round(status.PercentComplete, 1),
                    errorMessage = status.ErrorMessage,
                    timestamp = DateTime.UtcNow.ToString("O")
                };

                yield return FormatSSE("progress", JsonSerializer.Serialize(progressData));
            }

            // Check if download is complete
            if (status.Status is "Completed" or "Failed" or "Cancelled")
            {
                yield return FormatSSE("complete", JsonSerializer.Serialize(new { 
                    model = modelName, 
                    status = status.Status.ToLowerInvariant(),
                    finalProgress = status.PercentComplete,
                    errorMessage = status.ErrorMessage
                }));
                yield break;
            }

            // Wait before next check
            await Task.Delay(1000, cancellationToken);
        }
    }

    /// <summary>
    /// Safely gets download status without throwing exceptions.
    /// </summary>
    private async Task<OllamaDownloadRecord?> SafeGetDownloadStatus(string modelName, CancellationToken cancellationToken)
    {
        try
        {
            return await _repository.GetDownloadStatusAsync(modelName, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting download status for model {ModelName}", modelName);
            return null;
        }
    }

    /// <summary>
    /// Process the actual download with progress tracking
    /// </summary>
    private async Task ProcessDownloadAsync(string modelName, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Starting download for model {ModelName}", modelName);

            await foreach (var progress in _ollamaApi.PullModelWithProgressAsync(modelName, cancellationToken))
            {
                // Update database with progress
                await _repository.UpsertDownloadProgressAsync(new OllamaDownloadRecord
                {
                    ModelName = progress.ModelName,
                    Status = progress.IsComplete ? "Completed" : "Downloading",
                    BytesDownloaded = progress.BytesDownloaded,
                    TotalBytes = progress.TotalBytes,
                    PercentComplete = progress.PercentComplete,
                    ErrorMessage = progress.ErrorMessage
                }, cancellationToken);

                // If download is complete, sync model info
                if (progress.IsComplete && string.IsNullOrEmpty(progress.ErrorMessage))
                {
                    await SyncModelInfoAsync(modelName, cancellationToken);
                    break;
                }
                
                // If there's an error, mark as failed
                if (!string.IsNullOrEmpty(progress.ErrorMessage))
                {
                    await _repository.UpsertDownloadProgressAsync(new OllamaDownloadRecord
                    {
                        ModelName = modelName,
                        Status = "Failed",
                        BytesDownloaded = progress.BytesDownloaded,
                        TotalBytes = progress.TotalBytes,
                        PercentComplete = progress.PercentComplete,
                        ErrorMessage = progress.ErrorMessage
                    }, cancellationToken);
                    break;
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Download cancelled for model {ModelName}", modelName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Download failed for model {ModelName}", modelName);
            
            await _repository.UpsertDownloadProgressAsync(new OllamaDownloadRecord
            {
                ModelName = modelName,
                Status = "Failed",
                ErrorMessage = ex.Message
            }, cancellationToken);
        }
        finally
        {
            // Clean up tracking
            _activeDownloads.TryRemove(modelName, out var cts);
            cts?.Dispose();
        }
    }

    /// <summary>
    /// Sync model information after successful download
    /// </summary>
    private async Task SyncModelInfoAsync(string modelName, CancellationToken cancellationToken)
    {
        try
        {
            var modelInfo = await _ollamaApi.GetModelDetailsAsync(modelName, cancellationToken);
            if (modelInfo != null)
            {
                await _repository.UpsertModelAsync(new OllamaModelRecord
                {
                    Name = modelInfo.Name,
                    Size = modelInfo.Size,
                    ModifiedAt = modelInfo.ModifiedAt,
                    Family = modelInfo.Details?.Family,
                    ParameterSize = modelInfo.Details?.ParameterSize,
                    QuantizationLevel = modelInfo.Details?.QuantizationLevel,
                    Format = modelInfo.Details?.Format,
                    Template = modelInfo.Template,
                    Parameters = modelInfo.Parameters != null ? JsonSerializer.Serialize(modelInfo.Parameters) : null,
                    IsAvailable = true,
                    Status = "Ready"
                }, cancellationToken);

                _logger.LogInformation("Successfully synced model info for {ModelName}", modelName);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to sync model info for {ModelName}", modelName);
        }
    }

    /// <summary>
    /// Format data for Server-Sent Events
    /// </summary>
    private static string FormatSSE(string eventType, string data)
    {
        return $"event: {eventType}\ndata: {data}\n\n";
    }
}