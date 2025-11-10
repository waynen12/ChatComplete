using Microsoft.Extensions.AI;

namespace Knowledge.Api.Services;

/// <summary>
/// Modern embedding service using Microsoft.Extensions.AI directly with OpenAI
/// </summary>
public class SemanticKernelEmbeddingService : IEmbeddingGenerator<string, Embedding<float>>
{
    private readonly IEmbeddingGenerator<string, Embedding<float>> _embeddingGenerator;

    /// <summary>
    /// Initializes a new instance of the SemanticKernelEmbeddingService
    /// </summary>
    public SemanticKernelEmbeddingService(IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator)
    {
        _embeddingGenerator = embeddingGenerator ?? throw new ArgumentNullException(nameof(embeddingGenerator));
    }

    /// <summary>
    /// Gets the metadata for this embedding generator.
    /// </summary>
    public EmbeddingGeneratorMetadata Metadata => new("semantic-kernel-wrapper");

    /// <summary>
    /// Generates embeddings for a collection of strings.
    /// </summary>
    /// <param name="values">The strings to generate embeddings for.</param>
    /// <param name="options">Optional generation options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The generated embeddings.</returns>
    public async Task<GeneratedEmbeddings<Embedding<float>>> GenerateAsync(
        IEnumerable<string> values,
        EmbeddingGenerationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        return await _embeddingGenerator.GenerateAsync(values, options, cancellationToken);
    }

    /// <summary>
    /// Gets a service of the specified type.
    /// </summary>
    /// <param name="serviceType">The type of service to retrieve.</param>
    /// <param name="serviceKey">Optional service key.</param>
    /// <returns>The service instance if available, otherwise null.</returns>
    public object? GetService(Type serviceType, object? serviceKey = null)
    {
        return serviceType.IsInstanceOfType(this) ? this : _embeddingGenerator.GetService(serviceType, serviceKey);
    }

    /// <summary>
    /// Disposes resources used by the embedding service.
    /// </summary>
    public void Dispose()
    {
        _embeddingGenerator?.Dispose();
    }
}
