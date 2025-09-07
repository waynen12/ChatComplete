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