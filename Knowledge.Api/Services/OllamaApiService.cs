using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Knowledge.Api.Services;

/// <summary>
/// Service for interacting with the Ollama API for model management and discovery.
/// </summary>
public interface IOllamaApiService
{
    /// <summary>
    /// Gets the list of locally installed Ollama models via API.
    /// </summary>
    Task<List<OllamaModelDto>> GetInstalledModelsAsync(
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Downloads a model with progress tracking.
    /// </summary>
    Task<DownloadResultDto> PullModelAsync(
        string modelName,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Deletes an installed model.
    /// </summary>
    Task<bool> DeleteModelAsync(string modelName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets detailed information about a specific model.
    /// </summary>
    Task<OllamaModelInfoDto?> GetModelDetailsAsync(
        string modelName,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Downloads a model with streaming progress updates.
    /// </summary>
    IAsyncEnumerable<DownloadProgressDto> PullModelWithProgressAsync(
        string modelName,
        CancellationToken cancellationToken = default
    );
}

/// <summary>
/// HTTP-based client for Ollama API operations.
/// </summary>
public class OllamaApiService : IOllamaApiService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<OllamaApiService> _logger;
    private readonly string _ollamaBaseUrl;

    /// <summary>
    /// Initializes a new instance of the OllamaApiService.
    /// </summary>
    /// <param name="httpClient">The HTTP client for API requests.</param>
    /// <param name="logger">The logger instance.</param>
    /// <param name="configuration">The application configuration.</param>
    public OllamaApiService(
        HttpClient httpClient,
        ILogger<OllamaApiService> logger,
        IConfiguration configuration
    )
    {
        _httpClient = httpClient;
        _logger = logger;

        // Get Ollama base URL from ChatCompleteSettings configuration
        _ollamaBaseUrl =
            configuration.GetValue<string>("ChatCompleteSettings:OllamaBaseUrl")
            ?? "http://localhost:11434";

        _logger.LogInformation(
            "OllamaApiService configured with base URL: {BaseUrl}",
            _ollamaBaseUrl
        );

        // Set timeout for Ollama API calls (increased for tool calling)
        _httpClient.Timeout = TimeSpan.FromSeconds(180);
    }

    /// <inheritdoc />
    public async Task<List<OllamaModelDto>> GetInstalledModelsAsync(
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            var response = await _httpClient.GetAsync(
                $"{_ollamaBaseUrl}/api/tags",
                cancellationToken
            );

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Ollama API request failed with status: {StatusCode}",
                    response.StatusCode
                );
                return new List<OllamaModelDto>();
            }

            var jsonContent = await response.Content.ReadAsStringAsync(cancellationToken);
            var ollamaResponse = JsonSerializer.Deserialize<OllamaTagsResponse>(
                jsonContent,
                new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower }
            );

            if (ollamaResponse?.Models == null)
            {
                _logger.LogWarning("Ollama API returned unexpected response format");
                return new List<OllamaModelDto>();
            }

            return ollamaResponse
                .Models.Select(model => new OllamaModelDto
                {
                    Name = model.Name,
                    Size = model.Size,
                    ModifiedAt = model.ModifiedAt,
                    Details = model.Details,
                })
                .ToList();
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to connect to Ollama API at {BaseUrl}", _ollamaBaseUrl);
            return new List<OllamaModelDto>();
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "Ollama API request timed out");
            return new List<OllamaModelDto>();
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse Ollama API response");
            return new List<OllamaModelDto>();
        }
    }

    /// <inheritdoc />
    public async Task<DownloadResultDto> PullModelAsync(
        string modelName,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            var request = new { name = modelName };
            var response = await _httpClient.PostAsJsonAsync(
                $"{_ollamaBaseUrl}/api/pull",
                request,
                cancellationToken
            );

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning(
                    "Ollama pull request failed with status: {StatusCode}, Error: {Error}",
                    response.StatusCode,
                    errorContent
                );

                return new DownloadResultDto
                {
                    Success = false,
                    ErrorMessage = $"Failed to start download: {response.StatusCode}",
                    ModelName = modelName,
                };
            }

            // For simple pull, we just return success after the request completes
            // The actual download happens asynchronously on Ollama's side
            return new DownloadResultDto
            {
                Success = true,
                ModelName = modelName,
                TotalBytes = 0, // We don't know the size yet
            };
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to connect to Ollama API for pull request");
            return new DownloadResultDto
            {
                Success = false,
                ErrorMessage = "Failed to connect to Ollama service",
                ModelName = modelName,
            };
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "Ollama pull request timed out");
            return new DownloadResultDto
            {
                Success = false,
                ErrorMessage = "Request timed out",
                ModelName = modelName,
            };
        }
    }

    /// <inheritdoc />
    public async Task<bool> DeleteModelAsync(
        string modelName,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            var request = new { name = modelName };
            var content = JsonContent.Create(request);
            var response = await _httpClient.SendAsync(
                new HttpRequestMessage(HttpMethod.Delete, $"{_ollamaBaseUrl}/api/delete")
                {
                    Content = content,
                },
                cancellationToken
            );

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Ollama delete request failed with status: {StatusCode} for model: {ModelName}",
                    response.StatusCode,
                    modelName
                );
                return false;
            }

            _logger.LogInformation("Successfully deleted model: {ModelName}", modelName);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete model: {ModelName}", modelName);
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<OllamaModelInfoDto?> GetModelDetailsAsync(
        string modelName,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            var request = new { name = modelName };
            var response = await _httpClient.PostAsJsonAsync(
                $"{_ollamaBaseUrl}/api/show",
                request,
                cancellationToken
            );

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Ollama show request failed with status: {StatusCode} for model: {ModelName}",
                    response.StatusCode,
                    modelName
                );
                return null;
            }

            var jsonContent = await response.Content.ReadAsStringAsync(cancellationToken);
            var showResponse = JsonSerializer.Deserialize<OllamaShowResponse>(
                jsonContent,
                new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower }
            );

            if (showResponse == null)
            {
                _logger.LogWarning(
                    "Failed to parse show response for model: {ModelName}",
                    modelName
                );
                return null;
            }

            return new OllamaModelInfoDto
            {
                Name = modelName,
                Size = showResponse.Size,
                ModifiedAt = showResponse.ModifiedAt,
                Details = showResponse.Details,
                Template = showResponse.Template,
                Parameters = showResponse.Parameters,
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get model details for: {ModelName}", modelName);
            return null;
        }
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<DownloadProgressDto> PullModelWithProgressAsync(
        string modelName,
        [EnumeratorCancellation] CancellationToken cancellationToken = default
    )
    {
        await foreach (var progress in PullModelProgressInternal(modelName, cancellationToken))
        {
            yield return progress;
        }
    }

    /// <summary>
    /// Internal method to handle the streaming pull with proper error handling.
    /// </summary>
    private async IAsyncEnumerable<DownloadProgressDto> PullModelProgressInternal(
        string modelName,
        [EnumeratorCancellation] CancellationToken cancellationToken = default
    )
    {
        var request = new { name = modelName };

        HttpResponseMessage? response = null;
        Stream? stream = null;
        StreamReader? reader = null;

        // Track progress across multiple layers
        var layerProgress = new Dictionary<string, (long completed, long total)>();
        var lastReportedPercentage = 0.0;

        // Initialize response outside try block
        response = await SafePostRequest(request, cancellationToken);

        if (response == null || !response.IsSuccessStatusCode)
        {
            yield return new DownloadProgressDto
            {
                ModelName = modelName,
                Status = "failed",
                ErrorMessage =
                    response == null
                        ? "Failed to connect to Ollama"
                        : $"Failed to start download: {response.StatusCode}",
                IsComplete = true,
            };
            yield break;
        }

        stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        reader = new StreamReader(stream);

        await foreach (var line in ReadLinesAsync(reader, cancellationToken))
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;

            var progressUpdate = TryParseProgressLine(line);
            if (progressUpdate != null)
            {
                // Handle different status types
                if (progressUpdate.Status == "success")
                {
                    yield return new DownloadProgressDto
                    {
                        ModelName = modelName,
                        Status = "success",
                        BytesDownloaded = layerProgress.Values.Sum(v => v.completed),
                        TotalBytes = layerProgress.Values.Sum(v => v.total),
                        IsComplete = true,
                    };
                    yield break;
                }

                // Track progress for downloading layers
                if (progressUpdate.Status.Contains("downloading") && !string.IsNullOrEmpty(progressUpdate.Digest))
                {
                    layerProgress[progressUpdate.Digest] = (progressUpdate.Completed, progressUpdate.Total);
                    
                    // Calculate aggregate progress
                    var totalCompleted = layerProgress.Values.Sum(v => v.completed);
                    var totalBytes = layerProgress.Values.Sum(v => v.total);
                    var currentPercentage = totalBytes > 0 ? (totalCompleted * 100.0) / totalBytes : 0;

                    // Only report progress if it changed significantly (every 1%) to avoid spam
                    if (Math.Abs(currentPercentage - lastReportedPercentage) >= 1.0 || currentPercentage >= 100.0)
                    {
                        lastReportedPercentage = currentPercentage;
                        
                        yield return new DownloadProgressDto
                        {
                            ModelName = modelName,
                            Status = "downloading",
                            BytesDownloaded = totalCompleted,
                            TotalBytes = totalBytes,
                            IsComplete = false,
                        };
                    }
                }
                else if (progressUpdate.Status.Contains("verifying") || 
                         progressUpdate.Status.Contains("writing") || 
                         progressUpdate.Status.Contains("removing"))
                {
                    // Final stages - report as near completion
                    var totalCompleted = layerProgress.Values.Sum(v => v.completed);
                    var totalBytes = layerProgress.Values.Sum(v => v.total);
                    
                    yield return new DownloadProgressDto
                    {
                        ModelName = modelName,
                        Status = progressUpdate.Status,
                        BytesDownloaded = totalBytes, // Show as complete for final stages
                        TotalBytes = totalBytes,
                        IsComplete = false,
                    };
                }
            }
        }

        // Cleanup
        reader?.Dispose();
        stream?.Dispose();
        response?.Dispose();
    }

    /// <summary>
    /// Safely posts a request without throwing exceptions.
    /// </summary>
    private async Task<HttpResponseMessage?> SafePostRequest(
        object request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            return await _httpClient.PostAsJsonAsync(
                $"{_ollamaBaseUrl}/api/pull",
                request,
                cancellationToken
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error posting pull request to Ollama");
            return null;
        }
    }

    /// <summary>
    /// Helper method to safely parse progress lines without throwing exceptions.
    /// </summary>
    private OllamaPullProgress? TryParseProgressLine(string line)
    {
        try
        {
            return JsonSerializer.Deserialize<OllamaPullProgress>(
                line,
                new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower }
            );
        }
        catch (JsonException ex)
        {
            _logger.LogDebug(
                "Failed to parse progress line: {Line}, Error: {Error}",
                line,
                ex.Message
            );
            return null;
        }
    }

    /// <summary>
    /// Helper method to read lines asynchronously from a StreamReader.
    /// </summary>
    private static async IAsyncEnumerable<string> ReadLinesAsync(
        StreamReader reader,
        [EnumeratorCancellation] CancellationToken cancellationToken = default
    )
    {
        string? line;
        while ((line = await reader.ReadLineAsync()) != null)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return line;
        }
    }
}

/// <summary>
/// DTO representing an Ollama model returned by the API.
/// </summary>
public class OllamaModelDto
{
    /// <summary>
    /// The model name (e.g., "llama3.2:3b").
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Model size in bytes.
    /// </summary>
    public long Size { get; set; }

    /// <summary>
    /// When the model was last modified.
    /// </summary>
    [JsonPropertyName("modified_at")]
    public DateTime ModifiedAt { get; set; }

    /// <summary>
    /// Additional model details.
    /// </summary>
    public OllamaModelDetails? Details { get; set; }
}

/// <summary>
/// Additional details about an Ollama model.
/// </summary>
public class OllamaModelDetails
{
    /// <summary>
    /// Model format information.
    /// </summary>
    public string Format { get; set; } = string.Empty;

    /// <summary>
    /// Model family (e.g., "llama", "gemma").
    /// </summary>
    public string Family { get; set; } = string.Empty;

    /// <summary>
    /// Number of parameters.
    /// </summary>
    [JsonPropertyName("parameter_size")]
    public string ParameterSize { get; set; } = string.Empty;

    /// <summary>
    /// Quantization level.
    /// </summary>
    [JsonPropertyName("quantization_level")]
    public string QuantizationLevel { get; set; } = string.Empty;
}

/// <summary>
/// Response from Ollama's /api/tags endpoint.
/// </summary>
internal class OllamaTagsResponse
{
    /// <summary>
    /// List of installed models.
    /// </summary>
    public List<OllamaModelDto> Models { get; set; } = new();
}

/// <summary>
/// Result of a model download operation.
/// </summary>
public class DownloadResultDto
{
    /// <summary>
    /// Whether the download was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Error message if download failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// The model name that was downloaded.
    /// </summary>
    public string ModelName { get; set; } = string.Empty;

    /// <summary>
    /// Total bytes downloaded.
    /// </summary>
    public long TotalBytes { get; set; }
}

/// <summary>
/// Progress information during model download.
/// </summary>
public class DownloadProgressDto
{
    /// <summary>
    /// The model being downloaded.
    /// </summary>
    public string ModelName { get; set; } = string.Empty;

    /// <summary>
    /// Current download status.
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Bytes downloaded so far.
    /// </summary>
    public long BytesDownloaded { get; set; }

    /// <summary>
    /// Total bytes to download.
    /// </summary>
    public long TotalBytes { get; set; }

    /// <summary>
    /// Percentage complete (0-100).
    /// </summary>
    public double PercentComplete => TotalBytes > 0 ? (BytesDownloaded * 100.0) / TotalBytes : 0;

    /// <summary>
    /// Whether the download is complete.
    /// </summary>
    public bool IsComplete { get; set; }

    /// <summary>
    /// Error message if download failed.
    /// </summary>
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Detailed information about a specific model.
/// </summary>
public class OllamaModelInfoDto
{
    /// <summary>
    /// Model name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Model size in bytes.
    /// </summary>
    public long Size { get; set; }

    /// <summary>
    /// When the model was modified.
    /// </summary>
    public DateTime ModifiedAt { get; set; }

    /// <summary>
    /// Detailed model information.
    /// </summary>
    public OllamaModelDetails? Details { get; set; }

    /// <summary>
    /// Model template/prompt format.
    /// </summary>
    public string? Template { get; set; }

    /// <summary>
    /// Model parameters as a string (format changed in newer Ollama versions).
    /// </summary>
    public string? Parameters { get; set; }
}

/// <summary>
/// Response from Ollama's /api/show endpoint.
/// </summary>
internal class OllamaShowResponse
{
    /// <summary>
    /// Model size in bytes.
    /// </summary>
    public long Size { get; set; }

    /// <summary>
    /// When the model was modified.
    /// </summary>
    [JsonPropertyName("modified_at")]
    public DateTime ModifiedAt { get; set; }

    /// <summary>
    /// Model details.
    /// </summary>
    public OllamaModelDetails? Details { get; set; }

    /// <summary>
    /// Model template.
    /// </summary>
    public string? Template { get; set; }

    /// <summary>
    /// Model parameters as a string (format changed in newer Ollama versions).
    /// </summary>
    public string? Parameters { get; set; }
}

/// <summary>
/// Progress information from Ollama's streaming pull response.
/// </summary>
internal class OllamaPullProgress
{
    /// <summary>
    /// Current status of the pull operation.
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Digest/hash of the file being downloaded.
    /// </summary>
    public string? Digest { get; set; }

    /// <summary>
    /// Bytes completed so far.
    /// </summary>
    public long Completed { get; set; }

    /// <summary>
    /// Total bytes to download.
    /// </summary>
    public long Total { get; set; }
}
