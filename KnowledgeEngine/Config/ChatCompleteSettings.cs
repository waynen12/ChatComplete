namespace ChatCompletion.Config
{
    public class ChatCompleteSettings
    {
        public MongoAtlasSettings Atlas { get; set; } = new();
        public VectorStoreSettings VectorStore { get; set; } = new();

        public string OpenAIModel { get; set; } = "gpt-3.5-turbo";
        public string GoogleModel { get; set; } = "gemini-2.5-flash";
        
        public string OllamaBaseUrl  { get; set; } = "http://localhost:11434";
        public string OllamaModel    { get; set; } = "gemma3:12b";

        public string AnthropicModel { get; set; } = "claude-sonnet-4-20250514";

        // Enhanced embedding provider configuration
        public EmbeddingProvidersSettings EmbeddingProviders { get; set; } = new();
        public int ChunkCharacterLimit { get; set; } = 4096;
        public int ChunkLineTokens { get; set; }
        public int ChunkParagraphTokens { get; set; }
        public int ChunkOverlap { get; set; }
        public string FilePath { get; set; } = string.Empty;

        public int LogFileSizeLimit { get; set; } = 10485760; // 10 MB

        public string SystemPrompt { get; set; }
        public string SystemPromptWithCoding { get; set; }

        public double Temperature { get; set; } = 0.7;
        
        public int ChatMaxTurns { get; set; } = 12;   // assistant+user pairs ⇒ 24 msgs
        
        // Code fence protection settings
        public int MaxCodeFenceSize { get; set; } = 10240; // 10KB max per code block
        public bool TruncateOversizedCodeFences { get; set; } = true;
        
        // SQLite database configuration
        public string? DatabasePath { get; set; } = null; // null = use smart default
        
        // Backward compatibility properties
        public string EmbeddingProvider => EmbeddingProviders.ActiveProvider;
        public string TextEmbeddingModelName => EmbeddingProviders.GetActiveProvider().ModelName;
        public string OllamaEmbeddingModel => EmbeddingProviders.Ollama.ModelName;
    }

    /// <summary>
    /// Configuration for multiple embedding providers with their specific settings
    /// </summary>
    public class EmbeddingProvidersSettings
    {
        public string ActiveProvider { get; set; } = "OpenAI";
        public EmbeddingProviderConfig OpenAI { get; set; } = new()
        {
            ModelName = "text-embedding-ada-002",
            Dimensions = 1536,
            MinRelevanceScore = 0.6,
            MaxTokens = 8191,
            Description = "OpenAI text-embedding-ada-002 model"
        };
        public EmbeddingProviderConfig Ollama { get; set; } = new()
        {
            ModelName = "nomic-embed-text",
            Dimensions = 768,
            MinRelevanceScore = 0.3,
            MaxTokens = 2048,
            Description = "Ollama nomic-embed-text model with Matryoshka representation"
        };
        public EmbeddingProviderConfig Alternative { get; set; } = new()
        {
            ModelName = "all-MiniLM-L6-v2",
            Dimensions = 384,
            MinRelevanceScore = 0.25,
            MaxTokens = 512,
            Description = "Lightweight alternative embedding model"
        };

        /// <summary>
        /// Gets the configuration for the currently active provider
        /// </summary>
        public EmbeddingProviderConfig GetActiveProvider()
        {
            return ActiveProvider.ToLower() switch
            {
                "openai" => OpenAI,
                "ollama" => Ollama,
                "alternative" => Alternative,
                _ => throw new InvalidOperationException($"Unknown embedding provider: {ActiveProvider}")
            };
        }

        /// <summary>
        /// Gets configuration for a specific provider by name
        /// </summary>
        public EmbeddingProviderConfig GetProvider(string providerName)
        {
            return providerName.ToLower() switch
            {
                "openai" => OpenAI,
                "ollama" => Ollama,
                "alternative" => Alternative,
                _ => throw new InvalidOperationException($"Unknown embedding provider: {providerName}")
            };
        }

        /// <summary>
        /// Validates that dimensions match between collection and current provider
        /// </summary>
        public bool ValidateDimensions(string collectionName, int expectedDimensions)
        {
            var activeConfig = GetActiveProvider();
            if (activeConfig.Dimensions != expectedDimensions)
            {
                throw new InvalidOperationException(
                    $"Dimension mismatch for collection '{collectionName}': " +
                    $"Active provider '{ActiveProvider}' uses {activeConfig.Dimensions} dimensions, " +
                    $"but collection expects {expectedDimensions} dimensions. " +
                    $"Please switch to a compatible provider or recreate the collection."
                );
            }
            return true;
        }
    }

    /// <summary>
    /// Configuration for a specific embedding provider
    /// </summary>
    public class EmbeddingProviderConfig
    {
        public string ModelName { get; set; } = string.Empty;
        public int Dimensions { get; set; }
        public double MinRelevanceScore { get; set; }
        public int MaxTokens { get; set; }
        public string Description { get; set; } = string.Empty;
    }
}
