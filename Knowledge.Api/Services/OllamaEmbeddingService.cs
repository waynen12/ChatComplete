using System.Text;
using System.Text.Json;
using Microsoft.Extensions.AI;

namespace Knowledge.Api.Services;

/// <summary>
/// Ollama embedding service implementation using local Ollama API
/// </summary>
public class OllamaEmbeddingService : IEmbeddingGenerator<string, Embedding<float>>
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;
    private readonly string _model;

    /// <summary>
    /// Initializes a new instance of the OllamaEmbeddingService
    /// </summary>
    /// <param name="httpClient">The HTTP client to use for making requests</param>
    /// <param name="baseUrl">The Ollama API base URL (e.g., http://localhost:11434)</param>
    /// <param name="model">The embedding model name (e.g., nomic-embed-text)</param>
    public OllamaEmbeddingService(HttpClient httpClient, string baseUrl, string model)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _baseUrl = baseUrl?.TrimEnd('/') ?? throw new ArgumentNullException(nameof(baseUrl));
        _model = model ?? throw new ArgumentNullException(nameof(model));
    }

    /// <summary>
    /// Gets the metadata for this Ollama embedding generator.
    /// </summary>
    public EmbeddingGeneratorMetadata Metadata => new($"ollama-{_model}");

    /// <summary>
    /// Generates embeddings for a collection of strings using Ollama
    /// </summary>
    /// <param name="values">The strings to generate embeddings for</param>
    /// <param name="options">Options for generating the embeddings (not used by Ollama)</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation</param>
    /// <returns>A GeneratedEmbeddings containing the generated embeddings</returns>
    public async Task<GeneratedEmbeddings<Embedding<float>>> GenerateAsync(
        IEnumerable<string> values,
        EmbeddingGenerationOptions? options = null,
        CancellationToken cancellationToken = default
    )
    {
        var valuesList = values.ToList();
        if (valuesList.Count == 0)
        {
            return new GeneratedEmbeddings<Embedding<float>>([]);
        }

        var embeddings = new List<Embedding<float>>();

        // Process each text individually (Ollama API processes one at a time)
        foreach (var text in valuesList)
        {
            var embedding = await GenerateSingleEmbeddingAsync(text, cancellationToken);
            embeddings.Add(embedding);
        }

        return new GeneratedEmbeddings<Embedding<float>>(embeddings);
    }

    /// <summary>
    /// Generates a single embedding for a text string
    /// </summary>
    private async Task<Embedding<float>> GenerateSingleEmbeddingAsync(
        string text,
        CancellationToken cancellationToken
    )
    {
        var request = new OllamaEmbedRequest { Model = _model, Input = text };

        var json = JsonSerializer.Serialize(request, JsonSerializerOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync(
            $"{_baseUrl}/api/embed",
            content,
            cancellationToken
        );

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException(
                $"Ollama embedding request failed with status {response.StatusCode}: {errorContent}"
            );
        }

        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
        var embeddingResponse = JsonSerializer.Deserialize<OllamaEmbedResponse>(
            responseJson,
            JsonSerializerOptions
        );

        if (embeddingResponse?.Embeddings == null || embeddingResponse.Embeddings.Length == 0)
        {
            throw new InvalidOperationException("Ollama returned empty embeddings response");
        }

        // Convert double[] to float[] for compatibility with Microsoft.Extensions.AI
        var floatEmbedding = embeddingResponse.Embeddings.First().Select(d => (float)d).ToArray();
        return new Embedding<float>(floatEmbedding);
    }

    /// <summary>
    /// Gets a service of the specified type
    /// </summary>
    public object? GetService(Type serviceType, object? serviceKey = null)
    {
        return serviceType.IsInstanceOfType(this) ? this : null;
    }

    /// <summary>
    /// Disposes the service
    /// </summary>
    public void Dispose()
    {
        // HttpClient is managed externally, don't dispose it here
        GC.SuppressFinalize(this);
    }

    private static readonly JsonSerializerOptions JsonSerializerOptions =
        new() { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower, WriteIndented = false };

    /// <summary>
    /// Request model for Ollama embed API
    /// </summary>
    private class OllamaEmbedRequest
    {
        public string Model { get; set; } = string.Empty;
        public string Input { get; set; } = string.Empty;
    }

    /// <summary>
    /// Response model for Ollama embed API
    /// </summary>
    private class OllamaEmbedResponse
    {
        public double[][] Embeddings { get; set; } = [];
    }
}