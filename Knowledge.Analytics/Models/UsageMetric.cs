using Knowledge.Contracts.Types;

namespace Knowledge.Analytics.Models;

public record UsageMetric
{
    public string Id { get; init; } = Guid.NewGuid().ToString();
    public string? ConversationId { get; init; }
    public string? KnowledgeId { get; init; }
    public AiProvider Provider { get; init; }
    public string? ModelName { get; init; }
    public int InputTokens { get; init; }
    public int OutputTokens { get; init; }
    public int TotalTokens => InputTokens + OutputTokens;
    public double Temperature { get; init; }
    public bool UsedAgentCapabilities { get; init; }
    public int ToolExecutions { get; init; }
    public TimeSpan ResponseTime { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public string? ClientId { get; init; }
    public bool WasSuccessful { get; init; } = true;
    public string? ErrorMessage { get; init; }
}

public record ModelUsageStats
{
    public string ModelName { get; init; } = string.Empty;
    public AiProvider Provider { get; init; }
    public int ConversationCount { get; init; }
    public int TotalTokens { get; init; }
    public int AverageTokensPerRequest { get; init; }
    public TimeSpan AverageResponseTime { get; init; }
    public DateTime LastUsed { get; init; }
    public bool? SupportsTools { get; init; }
    public int SuccessfulRequests { get; init; }
    public int FailedRequests { get; init; }
    public double SuccessRate => TotalRequests > 0 ? (double)SuccessfulRequests / TotalRequests : 0;
    public int TotalRequests => SuccessfulRequests + FailedRequests;
}

public record KnowledgeUsageStats  
{
    public string KnowledgeId { get; init; } = string.Empty;
    public string KnowledgeName { get; init; } = string.Empty;
    public int DocumentCount { get; init; }
    public int ChunkCount { get; init; }
    public int ConversationCount { get; init; }
    public int QueryCount { get; init; }
    public DateTime LastQueried { get; init; }
    public DateTime CreatedAt { get; init; }
    public string VectorStore { get; init; } = string.Empty;
    public long TotalFileSize { get; init; }
}

public record ProviderAccountInfo
{
    public string Provider { get; init; } = string.Empty;
    public bool IsConnected { get; init; }
    public bool ApiKeyConfigured { get; init; }
    public DateTime? LastSyncAt { get; init; }
    public decimal? Balance { get; init; }
    public string? BalanceUnit { get; init; }
    public decimal MonthlyUsage { get; init; }
    public string? ErrorMessage { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}

public record ProviderUsageInfo
{
    public string Provider { get; init; } = string.Empty;
    public decimal TotalCost { get; init; }
    public int TotalRequests { get; init; }
    public double SuccessRate { get; init; }
    public Dictionary<string, ModelUsageBreakdown> ModelBreakdown { get; init; } = new();
    public DateTime PeriodStart { get; init; }
    public DateTime PeriodEnd { get; init; }
}

public record ModelUsageBreakdown
{
    public string ModelName { get; init; } = string.Empty;
    public int Requests { get; init; }
    public int TokensUsed { get; init; }
    public decimal Cost { get; init; }
}

public record ProviderInfo
{
    public string Provider { get; init; } = string.Empty;
    public bool IsConnected { get; init; }
    public decimal MonthlyCost { get; init; }
    public int RequestCount { get; init; }
    public double SuccessRate { get; init; }
}

public record ProviderSummary
{
    public int TotalProviders { get; init; }
    public int ConnectedProviders { get; init; }
    public decimal TotalMonthlyCost { get; init; }
    public int TotalRequests { get; init; }
    public double AverageSuccessRate { get; init; }
    public Dictionary<string, ProviderInfo> Providers { get; init; } = new();
    public Dictionary<string, ProviderBreakdown> ProviderBreakdown { get; init; } = new();
    public DateTime LastUpdated { get; init; }
}

public record ProviderBreakdown
{
    public decimal Cost { get; init; }
    public int Requests { get; init; }
    public double SuccessRate { get; init; }
    public bool IsConnected { get; init; }
}

/// <summary>
/// Ollama-specific usage and resource information
/// </summary>
public record OllamaUsageInfo
{
    public string Provider { get; init; } = "Ollama";
    public bool IsConnected { get; init; }
    public int TotalModels { get; init; }
    public long TotalDiskSpaceBytes { get; init; }
    public int TotalRequests { get; init; }
    public int TotalTokens { get; init; }
    public double AverageResponseTimeMs { get; init; }
    public double SuccessRate { get; init; }
    public int ToolEnabledModels { get; init; }
    public DateTime LastUpdated { get; init; } = DateTime.UtcNow;
    public IEnumerable<OllamaModelUsage> TopModels { get; init; } = [];
    public OllamaDownloadActivity RecentDownloads { get; init; } = new();
    public DateTime PeriodStart { get; init; }
    public DateTime PeriodEnd { get; init; }
}

/// <summary>
/// Ollama model inventory and resource usage
/// </summary>
public record OllamaModelInventory
{
    public int TotalModels { get; init; }
    public long TotalSizeBytes { get; init; }
    public int AvailableModels { get; init; }
    public int ToolEnabledModels { get; init; }
    public IEnumerable<OllamaModelInfo> Models { get; init; } = [];
    public Dictionary<string, int> ModelsByFamily { get; init; } = new();
    public DateTime LastSyncAt { get; init; }
}

/// <summary>
/// Download statistics and activity
/// </summary>
public record OllamaDownloadStats
{
    public int TotalDownloads { get; init; }
    public int CompletedDownloads { get; init; }
    public int FailedDownloads { get; init; }
    public double SuccessRate => TotalDownloads > 0 ? (double)CompletedDownloads / TotalDownloads : 0;
    public long TotalBytesDownloaded { get; init; }
    public double AverageDownloadTimeMinutes { get; init; }
    public IEnumerable<OllamaDownloadInfo> RecentDownloads { get; init; } = [];
    public DateTime PeriodStart { get; init; }
    public DateTime PeriodEnd { get; init; }
}

/// <summary>
/// Performance metrics by model
/// </summary>
public record OllamaModelPerformance
{
    public string ModelName { get; init; } = string.Empty;
    public int Requests { get; init; }
    public double AverageResponseTimeMs { get; init; }
    public double SuccessRate { get; init; }
    public int TotalTokens { get; init; }
    public double TokensPerSecond => AverageResponseTimeMs > 0 ? TotalTokens / (AverageResponseTimeMs / 1000.0) : 0;
    public bool SupportsTools { get; init; }
    public DateTime LastUsed { get; init; }
}

/// <summary>
/// Model usage information for top models
/// </summary>
public record OllamaModelUsage
{
    public string ModelName { get; init; } = string.Empty;
    public int Requests { get; init; }
    public int TotalTokens { get; init; }
    public double AverageResponseTimeMs { get; init; }
    public long SizeBytes { get; init; }
    public bool SupportsTools { get; init; }
    public DateTime LastUsed { get; init; }
}

/// <summary>
/// Model information for inventory
/// </summary>
public record OllamaModelInfo
{
    public string Name { get; init; } = string.Empty;
    public string? DisplayName { get; init; }
    public long SizeBytes { get; init; }
    public string? Family { get; init; }
    public string? ParameterSize { get; init; }
    public bool IsAvailable { get; init; }
    public bool SupportsTools { get; init; }
    public DateTime InstalledAt { get; init; }
    public DateTime? LastUsedAt { get; init; }
}

/// <summary>
/// Download information for recent activity
/// </summary>
public record OllamaDownloadInfo
{
    public string ModelName { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public long TotalBytes { get; init; }
    public DateTime StartedAt { get; init; }
    public DateTime? CompletedAt { get; init; }
    public string? ErrorMessage { get; init; }
    public double DurationMinutes => CompletedAt.HasValue 
        ? (CompletedAt.Value - StartedAt).TotalMinutes 
        : 0;
}

/// <summary>
/// Recent download activity summary
/// </summary>
public record OllamaDownloadActivity
{
    public int PendingDownloads { get; init; }
    public int CompletedToday { get; init; }
    public int FailedToday { get; init; }
    public IEnumerable<OllamaDownloadInfo> RecentDownloads { get; init; } = [];
}