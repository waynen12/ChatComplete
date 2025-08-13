using System.Text.Json;
using System.Text.Json.Serialization;

namespace Knowledge.Api.Services;

/// <summary>
/// Service for interacting with the Ollama API instead of executing shell commands.
/// </summary>
public interface IOllamaApiService
{
    /// <summary>
    /// Gets the list of locally installed Ollama models via API.
    /// </summary>
    Task<List<OllamaModelDto>> GetInstalledModelsAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// HTTP-based client for Ollama API operations.
/// </summary>
public class OllamaApiService : IOllamaApiService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<OllamaApiService> _logger;
    private readonly string _ollamaBaseUrl;

    public OllamaApiService(HttpClient httpClient, ILogger<OllamaApiService> logger, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _logger = logger;
        
        // Default to standard Ollama port, allow override via configuration
        _ollamaBaseUrl = configuration.GetValue<string>("OllamaApi:BaseUrl") ?? "http://localhost:11434";
        
        // Set timeout for Ollama API calls
        _httpClient.Timeout = TimeSpan.FromSeconds(30);
    }

    /// <inheritdoc />
    public async Task<List<OllamaModelDto>> GetInstalledModelsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync($"{_ollamaBaseUrl}/api/tags", cancellationToken);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Ollama API request failed with status: {StatusCode}", response.StatusCode);
                return new List<OllamaModelDto>();
            }

            var jsonContent = await response.Content.ReadAsStringAsync(cancellationToken);
            var ollamaResponse = JsonSerializer.Deserialize<OllamaTagsResponse>(jsonContent, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
            });

            if (ollamaResponse?.Models == null)
            {
                _logger.LogWarning("Ollama API returned unexpected response format");
                return new List<OllamaModelDto>();
            }

            return ollamaResponse.Models.Select(model => new OllamaModelDto
            {
                Name = model.Name,
                Size = model.Size,
                ModifiedAt = model.ModifiedAt,
                Details = model.Details
            }).ToList();
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