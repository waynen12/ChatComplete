using System.Text;
using System.Text.Json;
using Microsoft.Extensions.AI;

namespace Knowledge.Api.Services;

/// <summary>
/// Simple OpenAI embedding service implementation
/// </summary>
public class OpenAiEmbeddingService : IEmbeddingGenerator<string, Embedding<float>>
{
    private readonly string _apiKey;
    private readonly HttpClient _httpClient;
    private const string OpenAiEmbeddingUrl = "https://api.openai.com/v1/embeddings";

    /// <summary>
    /// Represents a simple OpenAI embedding service implementation.
    /// </summary>
    /// <param name="apiKey">The OpenAI API key.</param>
    /// <param name="httpClient">The HTTP client to use for making requests.</param>
    /// <exception cref="ArgumentNullException">Thrown when either the API key or HTTP client is null.</exception>
    public OpenAiEmbeddingService(string apiKey, HttpClient httpClient)
    {
        _apiKey = apiKey ?? throw new ArgumentNullException(nameof(apiKey));
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    /// <summary>
    /// Represents metadata for the OpenAI embedding service.
    /// </summary>
    public EmbeddingGeneratorMetadata Metadata => new("openai-embedding");

    /// <summary>
    /// Generates embeddings for a collection of strings.
    /// </summary>
    /// <param name="values">The strings to generate embeddings for.</param>
    /// <param name="options">Options for generating the embeddings.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>A generated embeddings object containing the embeddings for each input string.</returns>
    public async Task<GeneratedEmbeddings<Embedding<float>>> GenerateAsync(
        IEnumerable<string> values,
        EmbeddingGenerationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var valuesList = values.ToList();
        if (!valuesList.Any())
        {
            return new GeneratedEmbeddings<Embedding<float>>(Array.Empty<Embedding<float>>());
        }

        var request = new
        {
            input = valuesList,
            model = "text-embedding-ada-002"
        };

        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var httpRequest = new HttpRequestMessage(HttpMethod.Post, OpenAiEmbeddingUrl)
        {
            Content = content
        };
        httpRequest.Headers.Add("Authorization", $"Bearer {_apiKey}");

        var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
        var embeddingResponse = JsonSerializer.Deserialize<OpenAIEmbeddingResponse>(responseJson);

        var embeddings = embeddingResponse!.Data.Select(d => 
            new Embedding<float>(d.Embedding)).ToArray();

        return new GeneratedEmbeddings<Embedding<float>>(embeddings);
    }

    /// <summary>
    /// Gets a service of the specified type.
    /// </summary>
    /// <param name="serviceType">The type of service to retrieve.</param>
    /// <param name="serviceKey">Optional service key.</param>
    /// <returns>The service instance if available, otherwise null.</returns>
    public object? GetService(Type serviceType, object? serviceKey = null)
    {
        return serviceType.IsInstanceOfType(this) ? this : null;
    }

    /// <summary>
    /// Disposes resources used by the embedding service.
    /// </summary>
    public void Dispose()
    {
        // HttpClient is managed externally, don't dispose it here
    }

    private class OpenAIEmbeddingResponse
    {
        public OpenAIEmbeddingData[] Data { get; set; } = Array.Empty<OpenAIEmbeddingData>();
    }

    private class OpenAIEmbeddingData
    {
        public float[] Embedding { get; set; } = Array.Empty<float>();
    }
}
