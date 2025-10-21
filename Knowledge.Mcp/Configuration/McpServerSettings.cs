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
    /// Resources functionality configuration
    /// </summary>
    public ResourceSettings Resources { get; set; } = new();

    /// <summary>
    /// HTTP transport configuration
    /// </summary>
    public HttpTransportSettings HttpTransport { get; set; } = new();

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
/// Resources specific configuration settings
/// </summary>
public class ResourceSettings
{
    /// <summary>
    /// Enable caching for resources
    /// </summary>
    public bool EnableCaching { get; set; } = true;

    /// <summary>
    /// Cache duration in seconds
    /// </summary>
    public int CacheDurationSeconds { get; set; } = 300;

    /// <summary>
    /// Maximum number of resources per list operation
    /// </summary>
    public int MaxResourcesPerList { get; set; } = 1000;

    /// <summary>
    /// Enable document content in resource responses
    /// </summary>
    public bool EnableDocumentContent { get; set; } = true;

    /// <summary>
    /// Maximum document size in bytes
    /// </summary>
    public int MaxDocumentSizeBytes { get; set; } = 10485760;

    /// <summary>
    /// Enable resource subscriptions (for future use)
    /// </summary>
    public bool EnableResourceSubscriptions { get; set; } = false;

    /// <summary>
    /// Default MIME type for resources
    /// </summary>
    public string DefaultMimeType { get; set; } = "text/plain";

    /// <summary>
    /// Enable statistics aggregation for resources
    /// </summary>
    public bool EnableStatisticsAggregation { get; set; } = true;
}

/// <summary>
/// HTTP transport specific configuration settings
/// </summary>
public class HttpTransportSettings
{
    /// <summary>
    /// HTTP server port
    /// </summary>
    public int Port { get; set; } = 5001;

    /// <summary>
    /// HTTP server host (e.g., localhost, 0.0.0.0)
    /// </summary>
    public string Host { get; set; } = "localhost";

    /// <summary>
    /// Session timeout in minutes
    /// </summary>
    public int SessionTimeoutMinutes { get; set; } = 30;

    /// <summary>
    /// Enable stateless mode (session data in headers instead of server memory)
    /// </summary>
    public bool EnableStatelessMode { get; set; } = false;

    /// <summary>
    /// CORS configuration
    /// </summary>
    public CorsSettings Cors { get; set; } = new();

    /// <summary>
    /// OAuth 2.1 authentication configuration (Milestone #23)
    /// </summary>
    public OAuthSettings? OAuth { get; set; }
}

/// <summary>
/// CORS (Cross-Origin Resource Sharing) configuration
/// </summary>
public class CorsSettings
{
    /// <summary>
    /// Enable CORS
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Allowed origins (use ["*"] for any origin in development only)
    /// </summary>
    public string[] AllowedOrigins { get; set; } = new[]
    {
        "http://localhost:3000",
        "http://localhost:5173",
        "https://copilot.github.com"
    };

    /// <summary>
    /// Allow any origin (DEVELOPMENT ONLY - insecure for production)
    /// </summary>
    public bool AllowAnyOrigin { get; set; } = false;

    /// <summary>
    /// Allow credentials (required for OAuth flows)
    /// </summary>
    public bool AllowCredentials { get; set; } = true;

    /// <summary>
    /// Exposed headers for clients
    /// </summary>
    public string[] ExposedHeaders { get; set; } = new[]
    {
        "Content-Type",
        "Cache-Control",
        "X-Request-Id",
        "Mcp-Session-Id"
    };
}

/// <summary>
/// OAuth 2.1 authentication configuration (Milestone #23)
/// Reference: MCP Authorization Specification
/// </summary>
public class OAuthSettings
{
    /// <summary>
    /// Enable OAuth 2.1 authentication
    /// </summary>
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// Authorization server URL (OAuth 2.0 Authorization Server Metadata - RFC8414)
    /// </summary>
    public string? AuthorizationServerUrl { get; set; }

    /// <summary>
    /// Resource indicator (RFC 8707) - binds tokens to intended audience
    /// </summary>
    public string? ResourceIndicator { get; set; }

    /// <summary>
    /// Require PKCE (Proof Key for Code Exchange) - MUST be true per MCP spec
    /// </summary>
    public bool RequirePkce { get; set; } = true;

    /// <summary>
    /// Token validation settings
    /// </summary>
    public TokenValidationSettings TokenValidation { get; set; } = new();

    /// <summary>
    /// OAuth scopes required for MCP access
    /// </summary>
    public string[] RequiredScopes { get; set; } = new[] { "mcp:read", "mcp:execute" };
}

/// <summary>
/// Token validation configuration
/// </summary>
public class TokenValidationSettings
{
    /// <summary>
    /// Validate token audience
    /// </summary>
    public bool ValidateAudience { get; set; } = true;

    /// <summary>
    /// Validate token issuer
    /// </summary>
    public bool ValidateIssuer { get; set; } = true;

    /// <summary>
    /// Validate token lifetime
    /// </summary>
    public bool ValidateLifetime { get; set; } = true;

    /// <summary>
    /// Clock skew tolerance in seconds
    /// </summary>
    public int ClockSkewSeconds { get; set; } = 300;
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