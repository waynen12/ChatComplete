using Knowledge.Entities;

namespace Knowledge.Data.Interfaces;

/// <summary>
/// Repository interface for Ollama model management
/// </summary>
public interface IOllamaRepository
{
    Task<List<OllamaModelRecord>> GetInstalledModelsAsync(CancellationToken cancellationToken = default);
    Task UpsertModelAsync(OllamaModelRecord model, CancellationToken cancellationToken = default);
    Task MarkModelUsedAsync(string modelName, CancellationToken cancellationToken = default);
    Task<OllamaModelRecord?> GetModelAsync(string modelName, CancellationToken cancellationToken = default);
    Task UpdateSupportsToolsAsync(string modelName, bool supportsTools, CancellationToken cancellationToken = default);
    Task DeleteModelAsync(string modelName, CancellationToken cancellationToken = default);
    Task UpsertDownloadProgressAsync(OllamaDownloadRecord download, CancellationToken cancellationToken = default);
    Task<OllamaDownloadRecord?> GetDownloadStatusAsync(string modelName, CancellationToken cancellationToken = default);
    Task<List<OllamaDownloadRecord>> GetActiveDownloadsAsync(CancellationToken cancellationToken = default);
    Task<List<OllamaDownloadRecord>> GetDownloadHistoryAsync(DateTime since, CancellationToken cancellationToken = default);
    Task CleanupOldDownloadsAsync(CancellationToken cancellationToken = default);
}