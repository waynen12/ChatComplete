namespace Knowledge.Mcp.Constants;

/// <summary>
/// Constants for MCP tool configuration and validation.
/// All hardcoded values should be moved here to enable easy configuration changes.
/// </summary>
public static class McpToolConstants
{
    /// <summary>
    /// Configuration constants for search functionality
    /// </summary>
    public static class Search
    {
        /// <summary>
        /// Default maximum number of results per knowledge base
        /// </summary>
        public const int DefaultResultLimit = 5;
        
        /// <summary>
        /// Maximum allowed result limit to prevent resource exhaustion
        /// </summary>
        public const int MaxResultLimit = 50;
        
        /// <summary>
        /// Minimum allowed result limit
        /// </summary>
        public const int MinResultLimit = 1;
        
        /// <summary>
        /// Default minimum relevance score for search results
        /// </summary>
        public const double DefaultMinRelevance = 0.6;
        
        /// <summary>
        /// Minimum allowed relevance score
        /// </summary>
        public const double MinRelevanceScore = 0.0;
        
        /// <summary>
        /// Maximum allowed relevance score
        /// </summary>
        public const double MaxRelevanceScore = 1.0;
        
        /// <summary>
        /// Maximum text preview length for search results
        /// </summary>
        public const int MaxPreviewLength = 200;
        
        /// <summary>
        /// Text truncation suffix
        /// </summary>
        public const string TruncationSuffix = "...";
    }

    /// <summary>
    /// Configuration constants for model recommendation functionality
    /// </summary>
    public static class ModelRecommendation
    {
        /// <summary>
        /// Default number of popular models to return
        /// </summary>
        public const int DefaultModelCount = 3;
        
        /// <summary>
        /// Maximum number of models to return
        /// </summary>
        public const int MaxModelCount = 20;
        
        /// <summary>
        /// Minimum number of models to return
        /// </summary>
        public const int MinModelCount = 1;
        
        /// <summary>
        /// Default time period for model popularity analysis
        /// </summary>
        public const string DefaultTimePeriod = "monthly";
        
        /// <summary>
        /// Default provider filter (all providers)
        /// </summary>
        public const string DefaultProvider = "all";
        
        /// <summary>
        /// Default comparison focus area
        /// </summary>
        public const string DefaultComparisonFocus = "all";
        
        /// <summary>
        /// Valid time periods for model analysis
        /// </summary>
        public static readonly string[] ValidTimePeriods = new[]
        {
            "daily", "weekly", "monthly", "all-time"
        };
        
        /// <summary>
        /// Valid providers for filtering
        /// </summary>
        public static readonly string[] ValidProviders = new[]
        {
            "openai", "anthropic", "google", "ollama", "all"
        };
        
        /// <summary>
        /// Valid comparison focus areas
        /// </summary>
        public static readonly string[] ValidComparisonFocusAreas = new[]
        {
            "performance", "usage", "efficiency", "all"
        };
        
        /// <summary>
        /// Minimum number of models required for comparison
        /// </summary>
        public const int MinModelsForComparison = 2;
    }

    /// <summary>
    /// Configuration constants for knowledge analytics functionality
    /// </summary>
    public static class KnowledgeAnalytics
    {
        /// <summary>
        /// Default value for including detailed metrics
        /// </summary>
        public const bool DefaultIncludeMetrics = true;
        
        /// <summary>
        /// Default sort order for knowledge base summary
        /// </summary>
        public const string DefaultSortBy = "activity";
        
        /// <summary>
        /// Valid sort options for knowledge base summary
        /// </summary>
        public static readonly string[] ValidSortOptions = new[]
        {
            "activity", "size", "age", "alphabetical"
        };
        
        /// <summary>
        /// Default value for synchronization checking
        /// </summary>
        public const bool DefaultCheckSynchronization = true;
        
        /// <summary>
        /// Default value for including performance metrics
        /// </summary>
        public const bool DefaultIncludePerformanceMetrics = false;
        
        /// <summary>
        /// Default minimum usage threshold in days for storage optimization
        /// </summary>
        public const int DefaultMinUsageThresholdDays = 30;
        
        /// <summary>
        /// Default value for including cleanup suggestions
        /// </summary>
        public const bool DefaultIncludeCleanupSuggestions = true;
    }

    /// <summary>
    /// Configuration constants for system health functionality
    /// </summary>
    public static class SystemHealth
    {
        /// <summary>
        /// Default value for including detailed metrics
        /// </summary>
        public const bool DefaultIncludeDetailedMetrics = true;
        
        /// <summary>
        /// Default scope for system health checks
        /// </summary>
        public const string DefaultScope = "all";
        
        /// <summary>
        /// Default value for including recommendations
        /// </summary>
        public const bool DefaultIncludeRecommendations = true;
        
        /// <summary>
        /// Valid scope options for system health checks
        /// </summary>
        public static readonly string[] ValidScopes = new[]
        {
            "all", "critical-only", "quick", "database", "vector-store", "ai-services"
        };
        
        /// <summary>
        /// Valid component names for health checking
        /// </summary>
        public static readonly string[] ValidComponents = new[]
        {
            "sqlite", "qdrant", "ollama", "openai", "anthropic", "google", "googleai"
        };
    }

    /// <summary>
    /// General MCP tool configuration constants
    /// </summary>
    public static class General
    {
        /// <summary>
        /// Default Ollama base URL for MCP server
        /// </summary>
        public const string DefaultOllamaBaseUrl = "http://localhost:11434";
        
        /// <summary>
        /// Default embedding model name
        /// </summary>
        public const string DefaultEmbeddingModel = "nomic-embed-text";
        
        /// <summary>
        /// Default Qdrant gRPC port
        /// </summary>
        public const int DefaultQdrantGrpcPort = 6334;
        
        /// <summary>
        /// Default Qdrant REST API port
        /// </summary>
        public const int DefaultQdrantRestPort = 6333;
    }

    /// <summary>
    /// Error messages and suggestions
    /// </summary>
    public static class ErrorMessages
    {
        /// <summary>
        /// Common error suggestions for users
        /// </summary>
        public static readonly string[] CommonSuggestions = new[]
        {
            "Check MCP server logs for detailed error information",
            "Verify knowledge base system is healthy",
            "Ensure all required services are running and accessible"
        };
        
        /// <summary>
        /// Configuration error suggestions
        /// </summary>
        public static readonly string[] ConfigurationSuggestions = new[]
        {
            "Check if MCP server is properly configured",
            "Verify knowledge base system is running",
            "Ensure all required services are registered in DI container"
        };
        
        /// <summary>
        /// Parameter validation suggestions
        /// </summary>
        public static readonly string[] ParameterSuggestions = new[]
        {
            "Provide valid parameter values within allowed ranges",
            "Check parameter documentation for valid options",
            "Try again with different parameter values if configuration is correct"
        };
    }
}