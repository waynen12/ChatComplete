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

    public EmbeddingGeneratorMetadata Metadata => new("semantic-kernel-wrapper");

    public async Task<GeneratedEmbeddings<Embedding<float>>> GenerateAsync(
        IEnumerable<string> values, 
        EmbeddingGenerationOptions? options = null, 
        CancellationToken cancellationToken = default)
    {
        return await _embeddingGenerator.GenerateAsync(values, options, cancellationToken);
    }

    public object? GetService(Type serviceType, object? serviceKey = null)
    {
        return serviceType.IsInstanceOfType(this) ? this : _embeddingGenerator.GetService(serviceType, serviceKey);
    }

    public void Dispose()
    {
        _embeddingGenerator?.Dispose();
    }
}
