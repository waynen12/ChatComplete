namespace Knowledge.Mcp.Configuration;

/// <summary>
/// Configuration settings specific to the MCP server.
/// These settings control MCP tool behavior, limits, and defaults.
/// </summary>
public class McpServerSettings
{
    /// <summary>
    /// Configuration section name in appsettings.json
    /// </summary>
    public const string SectionName = "McpServerSettings";

    /// <summary>
    /// Search functionality configuration
    /// </summary>
    public SearchSettings Search { get; set; } = new();

    /// <summary>
    /// Model recommendation functionality configuration
    /// </summary>
    public ModelRecommendationSettings ModelRecommendation { get; set; } = new();

    /// <summary>
    /// Knowledge analytics functionality configuration
    /// </summary>
    public KnowledgeAnalyticsSettings KnowledgeAnalytics { get; set; } = new();

    /// <summary>
    /// System health functionality configuration
    /// </summary>
    public SystemHealthSettings SystemHealth { get; set; } = new();

    /// <summary>
    /// General MCP server configuration
    /// </summary>
    public GeneralSettings General { get; set; } = new();
}

/// <summary>
/// Search-specific configuration settings
/// </summary>
public class SearchSettings
{
    /// <summary>
    /// Default maximum number of results per knowledge base
    /// </summary>
    public int DefaultResultLimit { get; set; } = 5;

    /// <summary>
    /// Maximum allowed result limit to prevent resource exhaustion
    /// </summary>
    public int MaxResultLimit { get; set; } = 50;

    /// <summary>
    /// Default minimum relevance score for search results
    /// </summary>
    public double DefaultMinRelevance { get; set; } = 0.6;

    /// <summary>
    /// Maximum text preview length for search results
    /// </summary>
    public int MaxPreviewLength { get; set; } = 200;
}

/// <summary>
/// Model recommendation specific configuration settings
/// </summary>
public class ModelRecommendationSettings
{
    /// <summary>
    /// Default number of popular models to return
    /// </summary>
    public int DefaultModelCount { get; set; } = 3;

    /// <summary>
    /// Maximum number of models to return
    /// </summary>
    public int MaxModelCount { get; set; } = 20;

    /// <summary>
    /// Default time period for model popularity analysis
    /// </summary>
    public string DefaultTimePeriod { get; set; } = "monthly";

    /// <summary>
    /// Default provider filter
    /// </summary>
    public string DefaultProvider { get; set; } = "all";

    /// <summary>
    /// Default comparison focus area
    /// </summary>
    public string DefaultComparisonFocus { get; set; } = "all";
}

/// <summary>
/// Knowledge analytics specific configuration settings
/// </summary>
public class KnowledgeAnalyticsSettings
{
    /// <summary>
    /// Default value for including detailed metrics
    /// </summary>
    public bool DefaultIncludeMetrics { get; set; } = true;

    /// <summary>
    /// Default sort order for knowledge base summary
    /// </summary>
    public string DefaultSortBy { get; set; } = "activity";

    /// <summary>
    /// Default minimum usage threshold in days for storage optimization
    /// </summary>
    public int DefaultMinUsageThresholdDays { get; set; } = 30;
}

/// <summary>
/// System health specific configuration settings
/// </summary>
public class SystemHealthSettings
{
    /// <summary>
    /// Default value for including detailed metrics
    /// </summary>
    public bool DefaultIncludeDetailedMetrics { get; set; } = true;

    /// <summary>
    /// Default scope for system health checks
    /// </summary>
    public string DefaultScope { get; set; } = "all";

    /// <summary>
    /// Default value for including recommendations
    /// </summary>
    public bool DefaultIncludeRecommendations { get; set; } = true;
}

/// <summary>
/// General MCP server configuration settings
/// </summary>
public class GeneralSettings
{
    /// <summary>
    /// Ollama base URL for embedding services
    /// </summary>
    public string OllamaBaseUrl { get; set; } = "http://localhost:11434";

    /// <summary>
    /// Default embedding model name
    /// </summary>
    public string DefaultEmbeddingModel { get; set; } = "nomic-embed-text";

    /// <summary>
    /// Enable detailed error logging
    /// </summary>
    public bool EnableDetailedErrorLogging { get; set; } = true;

    /// <summary>
    /// Enable performance monitoring for MCP tools
    /// </summary>
    public bool EnablePerformanceMonitoring { get; set; } = true;
}